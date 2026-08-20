# PTC Hub

A real academic web platform built for Palestine Technical College – Deir El-Balah's Computer Systems Engineering department. Not a demo — a production system actively used by students, supervisors, and admins.
 

---

## Overview

PTC Hub centralizes course tracking, course materials, announcements, and student progress for a full engineering department — 47 courses across 4 academic years — with a permission system that actually enforces who can see and touch what.

## Tech Stack

| Layer      | Technology |
|------------|------------|
| Backend    | .NET 8 Web API |
| Database   | SQL Server 2022 |
| Data Access | ADO.NET (no ORM) |
| Auth       | JWT Bearer + BCrypt |
| Email      | Brevo REST API (no SDK) |
| Frontend   | Vanilla JS + HTML + CSS (Arabic RTL) |
| Hosting    | SmarterASP.NET (IIS) |

## Roles & Permissions

- **Student** — sees their own year's content plus general announcements
- **Supervisor** — scoped to a single year: manages that year's students, announcements, and course files
- **Admin** — either year-scoped or general (global access to everything)

Permission checks are enforced **server-side**, not just hidden in the UI. Built-in safeguards:
- The last general admin can't be removed
- Admins can't change their own role
- A year supervisor can't self-promote to general admin

## Features

- **Course Management** — full catalog of 47 courses across 4 years; students can favorite courses
- **Course Files** — staff uploads are auto-approved; student-suggested files go into a pending queue for supervisor approval, scoped by admin permissions
- **Announcements** — general (all students) or year-specific, filtered server-side so students can't reach other years' content
- **Progress Tracking** — per-course status (done / doing / none), hours logged, and notes
- **Password Management** — admin-triggered temporary password reset; self-service forgot-password via email OTP (15-minute expiry, 5 attempts); change password via current password
- **Student Management** — filter students by year; bulk-move students between years; year 5 marks graduation

## Skills Demonstrated

REST API design · JWT authentication · Role-based access control · ADO.NET · SQL Server · Third-party API integration · Application security · Institutional system design & deployment

---

**Author:** Yaseen Eimirat — [github.com/YeasenEimirat](https://github.com/YeasenEimirat)
