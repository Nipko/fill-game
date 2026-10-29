package com.amazonfill.lockguard;

import android.app.Application;
import android.content.ContentProvider;
import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.net.Uri;

/**
 * Engancha {@link AfLockGuard} al arrancar el proceso, antes de crear ninguna actividad y sin sustituir la de Unity. Sin
 * exportar; no guarda ni sirve datos (todas sus consultas devuelven null o 0).
 */
public final class AfLockGuardProvider extends ContentProvider {
    @Override
    public boolean onCreate() {
        Context context = getContext();
        Context app = context == null ? null : context.getApplicationContext();
        if (app instanceof Application) {
            AfLockGuard.install((Application) app);
        }

        return true;
    }

    @Override
    public Cursor query(Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder) {
        return null;
    }

    @Override
    public String getType(Uri uri) {
        return null;
    }

    @Override
    public Uri insert(Uri uri, ContentValues values) {
        return null;
    }

    @Override
    public int delete(Uri uri, String selection, String[] selectionArgs) {
        return 0;
    }

    @Override
    public int update(Uri uri, ContentValues values, String selection, String[] selectionArgs) {
        return 0;
    }
}
