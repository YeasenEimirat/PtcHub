// ============================================================
//  طبقة المصادقة والمزامنة — نسخة .NET API
//  بديلة لنسخة Supabase. نفس أسماء الدوال، فالصفحات لا تتغيّر.
//  يتطلّب تحميل api-config.js قبله.
// ============================================================
const PTCAuth = (function () {
    const TOKEN_KEY = 'ptc_token';
    const MIGRATED_KEY = 'ptc_migrated';
    const LAST_UID_KEY = 'ptc_last_uid';

    // ============================================================
    //  الكاش المحلي (رحلة الطالب + مساقاتي) تبع المتصفّح، مش تبع الحساب.
    //  بدون تنضيفه، الطالب اللي بيسجّل دخول بعدك على نفس الجهاز
    //  بيشوف بياناتك. هاي كانت تسريب حقيقي.
    // ============================================================
    function clearLocalCache() {
        try {
            const kill = [];
            for (let i = 0; i < localStorage.length; i++) {
                const k = localStorage.key(i);
                if (k && (k.indexOf('jr_') === 0 || k === 'ptc_mycourses' || k === MIGRATED_KEY)) kill.push(k);
            }
            kill.forEach(function (k) { localStorage.removeItem(k); });
        } catch (e) { }
    }

    // لو الحساب الحالي مختلف عن آخر حساب استعمل هذا المتصفّح → امسح الكاش
    function guardCacheOwner() {
        if (!user) return;
        try {
            const last = localStorage.getItem(LAST_UID_KEY);
            if (last && last !== String(user.id)) clearLocalCache();
            localStorage.setItem(LAST_UID_KEY, String(user.id));
        } catch (e) { }
    }
    let user = null;
    let profile = null;
    let ready = false;

    // ---------- إدارة التوكن ----------
    function getToken() {
        try { return localStorage.getItem(TOKEN_KEY); } catch (e) { return null; }
    }
    function setToken(t) {
        try { t ? localStorage.setItem(TOKEN_KEY, t) : localStorage.removeItem(TOKEN_KEY); } catch (e) { }
    }

    // ---------- نداء موحّد للـ API ----------
    async function api(path, { method = 'GET', body = null, auth = true } = {}) {
        const headers = { 'Content-Type': 'application/json' };
        const token = getToken();
        if (auth && token) headers['Authorization'] = 'Bearer ' + token;

        let res;
        try {
            res = await fetch(API_CONFIG.baseUrl + path, {
                method, headers,
                body: body ? JSON.stringify(body) : null
            });
        } catch (e) {
            throw new Error('تعذّر الوصول إلى الخادم.');
        }

        if (res.status === 401 && auth && token) {
            setToken(null); user = null; profile = null; emit();
            throw new Error('انتهت الجلسة، سجّل الدخول من جديد.');
        }

        let json = null;
        try { json = await res.json(); } catch (e) { }

        if (!res.ok || (json && json.success === false)) {
            // الباكند صار يرجّع { success, message } لكل الأخطاء (حتى أخطاء التحقّق)
            // بس لو صار شي غير متوقّع، منعطي رسالة مفيدة حسب رمز الحالة
            let m = (json && json.message);
            if (!m) {
                if (res.status === 429) m = 'محاولات كثيرة. انتظر دقيقة وحاول من جديد.';
                else if (res.status === 403) m = 'ما عندك صلاحية لهذا الإجراء.';
                else if (res.status === 404) m = 'العنصر المطلوب غير موجود.';
                else if (res.status >= 500) m = 'الخادم واجه مشكلة. حاول بعد شوي.';
                else m = 'تعذّر إتمام الطلب (' + res.status + ').';
            }
            const err = new Error(m);
            err.status = res.status;
            throw err;
        }
        return json ? json.data : null;
    }

    // ---------- تحويل من camelCase إلى snake_case ----------
    function mapProfile(p) {
        if (!p) return null;
        return {
            id: p.id, full_name: p.fullName, student_id: p.studentId,
            email: p.email, year: p.year, role: p.role,
            scope_year: p.scopeYear ?? null,   // نطاق مسؤولية الطاقم (null = كل السنوات)
            created_at: p.createdAt
        };
    }
    function mapFile(f) {
        if (!f) return null;
        return {
            id: f.id, course_code: f.courseCode, title: f.title, url: f.url,
            kind: f.kind, size_label: f.sizeLabel, sort_order: f.sortOrder, created_at: f.createdAt
        };
    }

    function emit() {
        window.dispatchEvent(new CustomEvent('ptc-auth-change', { detail: { user, profile } }));
    }

    // ---------- استعادة الجلسة عند فتح الموقع ----------
    async function init() {
        if (!getToken()) { ready = true; emit(); return; }
        try {
            const p = await api('/auth/me');
            profile = mapProfile(p);
            user = { id: profile.id, email: profile.email, full_name: profile.full_name };
            guardCacheOwner();
        } catch (e) {
            setToken(null); user = null; profile = null;
        }
        ready = true; emit();
    }

    // ---------- عمليات الحساب ----------
    async function signUp({ email, password, fullName, studentId, year }) {
        const result = await api('/auth/register', {
            auth: false, method: 'POST',
            // الرقم الجامعي [Required] في الباكند — منبعته نصاً مقصوصاً، مش null
            body: {
                email, password, fullName,
                studentId: (studentId || '').trim(),
                year: year ? Number(year) : null
            }
        });
        setToken(result.token);
        await init();
        return { user, profile };
    }

    async function signIn({ email, password }) {
        const result = await api('/auth/login', {
            auth: false, method: 'POST', body: { email, password }
        });
        setToken(result.token);
        await init();
        return { user, profile };
    }

    async function signOut() {
        clearLocalCache();               // ما منخلّي بياناتنا للي بعدنا
        try { localStorage.removeItem(LAST_UID_KEY); } catch (e) { }
        setToken(null); user = null; profile = null; emit();
    }

    async function resetPassword(email) {
        throw new Error('استعادة كلمة السر غير مفعّلة بعد.');
    }

    async function updateProfile(fields) {
        const p = await api('/auth/profile', {
            method: 'PUT',
            // السنة ما عادت تنبعت من هون — الباكند بيتجاهلها أصلاً.
            // تعديلها من لوحة التحكم عبر changeStudentYear.
            body: {
                fullName: fields.full_name ?? profile?.full_name,
                studentId: fields.student_id ?? profile?.student_id
            }
        });
        profile = mapProfile(p); emit();
        return profile;
    }

    // ---------- مساقاتي ----------
    async function getMyCourses() {
        if (!user) { try { return JSON.parse(localStorage.getItem('ptc_mycourses') || '[]'); } catch (e) { return []; } }
        return await api('/mycourses');
    }
    async function addMyCourse(code) {
        if (!user) {
            const a = await getMyCourses(); if (!a.includes(code)) a.push(code);
            try { localStorage.setItem('ptc_mycourses', JSON.stringify(a)); } catch (e) { }
            return a;
        }
        return await api('/mycourses', { method: 'POST', body: { courseCode: code } });
    }
    async function removeMyCourse(code) {
        if (!user) {
            const a = (await getMyCourses()).filter(x => x !== code);
            try { localStorage.setItem('ptc_mycourses', JSON.stringify(a)); } catch (e) { }
            return a;
        }
        return await api('/mycourses?code=' + encodeURIComponent(code), { method: 'DELETE' });
    }

    // ---------- رحلة الطالب ----------
    async function getProgress(code) {
        if (!user) { try { return JSON.parse(localStorage.getItem('jr_' + code) || 'null'); } catch (e) { return null; } }
        return await api('/progress/single?code=' + encodeURIComponent(code));
    }
    async function saveProgress(code, obj) {
        if (!user) { try { localStorage.setItem('jr_' + code, JSON.stringify(obj)); } catch (e) { } return; }
        await api('/progress?code=' + encodeURIComponent(code), { method: 'PUT', body: { data: obj } });
    }
    async function getAllProgress() {
        if (!user) {
            const out = {};
            for (let i = 0; i < localStorage.length; i++) {
                const k = localStorage.key(i);
                if (k && k.startsWith('jr_') && !k.startsWith('jr_draft_')) {
                    try { out[k.slice(3)] = JSON.parse(localStorage.getItem(k)); } catch (e) { }
                }
            }
            return out;
        }
        return await api('/progress');
    }

    // ---------- ملفات المساقات (للطلاب) ----------
    async function getCourseFiles(code) {
        try {
            const list = await api('/files?courseCode=' + encodeURIComponent(code), { auth: false });
            return (list || []).map(mapFile);
        } catch (e) { return []; }
    }

    // ---------- لوحة التحكم: الملفات ----------
    async function getAllCourseFiles(filterCode) {
        const q = filterCode ? '?courseCode=' + encodeURIComponent(filterCode) : '';
        const list = await api('/files' + q, { auth: false });
        return (list || []).map(mapFile);
    }
    async function addCourseFile(f) {
        const created = await api('/files', {
            method: 'POST',
            body: {
                courseCode: f.course_code, title: f.title, url: f.url,
                kind: f.kind || 'pdf', sizeLabel: f.size_label || null, sortOrder: f.sort_order || 0
            }
        });
        // الـ Controller صار يرجّع الكائن كاملاً (مش { id } بس)
        return mapFile(created);
    }
    function deleteCourseFile(id) { return api('/files/' + id, { method: 'DELETE' }); }

    // ---------- لوحة التحكم: الإعلانات ----------
    async function getAnnouncements() {
        // ما عاد منبعت السنة من هون. السيرفر بيقرأها من حساب المستخدم نفسه،
        // فالطالب ما بيقدر يشوف إعلانات سنة تانية ولو عبث بالرابط.
        const list = await api('/announcements', { auth: !!getToken() });
        return (list || []).map(mapAnnouncement);
    }
    function mapAnnouncement(a) {
        if (!a) return null;
        return {
            id: a.id, title: a.title, body: a.body, active: a.active,
            year: a.year, created_at: a.createdAt
        };
    }
    async function addAnnouncement(a) {
        const created = await api('/announcements', {
            method: 'POST',
            body: {
                title: a.title, body: a.body || null,
                active: a.active !== false,
                year: (a.year === undefined || a.year === null || a.year === '') ? null : Number(a.year)
            }
        });
        return mapAnnouncement(created);
    }
    function deleteAnnouncement(id) { return api('/announcements/' + id, { method: 'DELETE' }); }

    // ---------- لوحة التحكم: الطلاب والصلاحيات ----------
    async function getStudents() {
        const list = await api('/admin/students');
        return (list || []).map(mapProfile);
    }
    // نسيت كلمة السر
    async function forgotPassword(email) {
        return await api('/auth/forgot-password', {
            method: 'POST', body: { email }, auth: false
        });
    }

    // إعادة تعيين كلمة السر بالـ OTP
    async function resetPassword(email, otp, newPassword) {
        return await api('/auth/reset-password', {
            method: 'POST', body: { email, otp, newPassword }, auth: false
        });
    }

    // تغيير كلمة السر (المستخدم يعرف القديمة)
    async function changePassword(currentPassword, newPassword) {
        return await api('/auth/change-password', {
            method: 'PUT', body: { currentPassword, newPassword }
        });
    }

    // تصفير كلمة سر طالب (من لوحة التحكم)
    async function adminResetPassword(userId) {
        return await api('/admin/reset-password', {
            method: 'POST', body: { userId }
        });
    }

    // إرسال ملف من طالب (بيحتاج موافقة)
    async function submitFile(courseCode, title, url, kind) {
        return await api('/files/submit', {
            method: 'POST', body: { courseCode, title, url, kind: kind || 'link' }
        });
    }

    // جلب الملفات المعلّقة (للطاقم)
    async function getPendingFiles() {
        return await api('/files/pending');
    }

    // موافقة أو رفض ملف
    async function reviewFile(id, decision) {
        return await api('/files/' + id + '/review', {
            method: 'PUT', body: { decision }
        });
    }

    // تعديل سنة طالب (الأدمن العام فقط)
    async function changeStudentYear(userId, year) {
        return await api('/admin/student-year', {
            method: 'PUT',
            body: {
                userId: userId,
                year: (year === undefined || year === null || year === '') ? null : Number(year)
            }
        });
    }

    // نقل مجموعة طلاب لسنة (الأدمن العام فقط)
    async function bulkChangeYear(userIds, year) {
        return await api('/admin/students/year', {
            method: 'PUT',
            body: {
                userIds: userIds,
                year: (year === undefined || year === null || year === '') ? null : Number(year)
            }
        });
    }

    async function changeRoleByEmail(email, role, scopeYear) {
        const students = await getStudents();
        const target = students.find(s => (s.email || '').toLowerCase() === email.toLowerCase());
        if (!target) throw new Error('لا يوجد مستخدم بهذا البريد (يجب أن يسجّل أولاً).');
        return await api('/admin/role', {
            method: 'PUT',
            body: {
                userId: target.id,
                role: role,
                // فاضي أو صفر → null، يعني أدمن عام على كل السنوات
                scopeYear: (scopeYear === undefined || scopeYear === null || scopeYear === '')
                    ? null : Number(scopeYear)
            }
        });
    }

    // بيرجّع true لو الترحيل صار قبل هيك (فما في داعي نعيده)
    function alreadyMigrated() {
        try { return localStorage.getItem(MIGRATED_KEY) === '1'; } catch (e) { return false; }
    }

    async function migrateLocalToCloud() {
        if (!user) return { courses: 0, progress: 0, skipped: true };

        // بدون هذا الفحص، الترحيل كان بيشتغل كل مرة تسجّل فيها دخول —
        // فالمساقات اللي حذفتها كانت بترجع، والتقدّم القديم بيدهس الجديد.
        if (alreadyMigrated()) return { courses: 0, progress: 0, skipped: true };

        let courses = 0, prog = 0;
        const usedKeys = [];

        try {
            const local = JSON.parse(localStorage.getItem('ptc_mycourses') || '[]');
            for (const code of local) {
                try { await addMyCourse(code); courses++; } catch (e) { }
            }
        } catch (e) { }

        for (let i = 0; i < localStorage.length; i++) {
            const k = localStorage.key(i);
            if (k && k.startsWith('jr_') && !k.startsWith('jr_draft_')) {
                try {
                    await saveProgress(k.slice(3), JSON.parse(localStorage.getItem(k)));
                    usedKeys.push(k);
                    prog++;
                } catch (e) { }
            }
        }

        // نجح الترحيل → نعلّم عليه ومننضّف النسخ المحلية حتى ما تتضارب مع السحابة
        try {
            localStorage.setItem(MIGRATED_KEY, '1');
            localStorage.removeItem('ptc_mycourses');
            usedKeys.forEach(function (k) { try { localStorage.removeItem(k); } catch (e) { } });
        } catch (e) { }

        return { courses, progress: prog, skipped: false };
    }

    return {
        init, enabled: () => true,
        signUp, signIn, signOut, resetPassword, updateProfile,
        getMyCourses, addMyCourse, removeMyCourse,
        getProgress, saveProgress, getAllProgress,
        getCourseFiles, migrateLocalToCloud, alreadyMigrated, clearLocalCache,
        getAllCourseFiles, addCourseFile, deleteCourseFile,
        getAnnouncements, addAnnouncement, deleteAnnouncement,
        getStudents, changeRoleByEmail, changeStudentYear, bulkChangeYear,
        forgotPassword, resetPassword, changePassword, adminResetPassword,
        submitFile, getPendingFiles, reviewFile,
        get user() { return user; },
        get profile() { return profile; },
        get isReady() { return ready; },
        isStaff: () => !!profile && (profile.role === 'admin' || profile.role === 'supervisor'),
        isAdmin: () => !!profile && profile.role === 'admin',

        // نطاق سنة الطاقم: رقم 1..4، أو null يعني مسؤول عن كل السنوات
        get scopeYear() { return (profile && profile.scope_year) ? Number(profile.scope_year) : null; },

        // أدمن عام = أدمن بلا نطاق سنة (هو الوحيد اللي بيوزّع الصلاحيات)
        isSuperAdmin: () => !!profile && profile.role === 'admin' && !profile.scope_year,

        // مسؤول سنة واحدة (أدمن سنة أو مشرف سنة)
        isScopedStaff: () => !!profile && (profile.role === 'admin' || profile.role === 'supervisor') && !!profile.scope_year
    };
})();

window.addEventListener('DOMContentLoaded', () => { PTCAuth.init(); });