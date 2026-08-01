// ============================================================
//  إعدادات الاتصال بالباكند (.NET API)
//  هذا الملف يحلّ محلّ supabase-config.js
// ============================================================
const API_CONFIG = {
    // بما أن الواجهة والباكند على نفس السيرفر، نترك الرابط فارغاً
    // فيصبح المسار نسبياً (/api) ويعمل محلياً وعلى الاستضافة تلقائياً
    baseUrl: "/api"
};

const API_READY = true;

// ============================================================
//  دوال الحماية من XSS
//  أي نص جاي من القاعدة أو من مستخدم ثاني لازم يمرّ من هنا
//  قبل ما ينحط داخل innerHTML.
// ============================================================

// تهريب النص لعرضه كمحتوى داخل HTML
function esc(v) {
    if (v === null || v === undefined) return '';
    return String(v)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

// تهريب النص لوضعه داخل خاصية (attribute) بين علامتي تنصيص
function escAttr(v) {
    return esc(v);
}

// فحص الرابط: بنسمح فقط بـ http/https والروابط النسبية.
// بيمنع javascript: و data: — وهي الطريقة الكلاسيكية لتنفيذ كود عبر href.
function safeUrl(v) {
    const s = String(v === null || v === undefined ? '' : v).trim();
    if (!s) return '#';
    if (/^(https?:)?\/\//i.test(s)) return esc(s);   // مطلق
    if (/^[\w\-./?#=&%]+$/.test(s)) return esc(s);   // نسبي بسيط
    return '#';
}

// متاحة عالمياً لكل الصفحات
window.esc = esc;
window.escAttr = escAttr;
window.safeUrl = safeUrl;
