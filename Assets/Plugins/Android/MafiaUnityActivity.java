package com.mafiaunity.android;

import android.app.Activity;
import android.content.Intent;
import android.content.Context;
import android.content.ContentResolver;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.provider.DocumentsContract;
import android.provider.OpenableColumns;
import android.util.Log;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.util.Locale;

public class MafiaUnityActivity extends UnityPlayerActivity {
    private static final int REQUEST_MAFIA_FOLDER = 41090;
    private static final String TAG = "MafiaUnityData";
    private static String destinationPath;

    public static void openMafiaFolderPicker(String destinationPath) {
        Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            return;
        }

        MafiaUnityActivity.destinationPath = destinationPath;
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT_TREE);
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        intent.addFlags(Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION);
        intent.addFlags(Intent.FLAG_GRANT_PREFIX_URI_PERMISSION);
        
        activity.startActivityForResult(intent, REQUEST_MAFIA_FOLDER);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode != REQUEST_MAFIA_FOLDER) {
            return;
        }

        if (resultCode != RESULT_OK || data == null || data.getData() == null) {
            sendResult(false, "cancelled");
            return;
        }

        Uri treeUri = data.getData();
        try {
            int takeFlags = data.getFlags() &
                    (Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION);
            getContentResolver().takePersistableUriPermission(treeUri, takeFlags);
        } catch (Exception ignored) {
        }

        final String destination = destinationPath;
        new Thread(new Runnable() {
            @Override public void run() {
                try {
                    File out = new File(destination);
                    if (!out.exists() && !out.mkdirs()) {
                        sendResult(false, "Cannot create data directory");
                        return;
                    }

                    int copied = copyDtaFiles(treeUri, out);
                    if (!hasRequiredData(out)) {
                        sendResult(false, "No valid Mafia DTA data found. Select the original Mafia: The City of Lost Heaven folder.");
                        return;
                    }

                    Log.i(TAG, "Copied " + copied + " Mafia DTA files to " + out.getAbsolutePath());
                    sendResult(true, out.getAbsolutePath());
                } catch (Throwable t) {
                    Log.e(TAG, "Import failed", t);
                    sendResult(false, "Import failed: " + t.getClass().getSimpleName());
                }
            }
        }).start();
    }

    private int copyDtaFiles(Uri treeUri, File destination) throws Exception {
        int count = 0;
        String rootId = DocumentsContract.getTreeDocumentId(treeUri);
        Uri children = DocumentsContract.buildChildDocumentsUriUsingTree(treeUri, rootId);

        Cursor cursor = getContentResolver().query(
                children,
                new String[] {
                        DocumentsContract.Document.COLUMN_DOCUMENT_ID,
                        DocumentsContract.Document.COLUMN_DISPLAY_NAME,
                        DocumentsContract.Document.COLUMN_MIME_TYPE
                },
                null, null, null);

        if (cursor == null) return 0;

        try {
            while (cursor.moveToNext()) {
                String id = cursor.getString(0);
                String name = cursor.getString(1);
                String mime = cursor.getString(2);
                Uri child = DocumentsContract.buildDocumentUriUsingTree(treeUri, id);

                if (DocumentsContract.Document.MIME_TYPE_DIR.equals(mime)) {
                    count += copyDtaFiles(child, destination);
                } else if (name != null && name.toLowerCase(Locale.US).endsWith(".dta")) {
                    File target = new File(destination, new File(name).getName());
                    copyUriToFile(child, target);
                    count++;
                }
            }
        } finally {
            cursor.close();
        }
        return count;
    }

    private void copyUriToFile(Uri uri, File target) throws Exception {
        ContentResolver resolver = getContentResolver();
        InputStream in = resolver.openInputStream(uri);
        if (in == null) throw new IllegalStateException("Cannot open " + uri);

        FileOutputStream out = new FileOutputStream(target, false);
        byte[] buffer = new byte[1024 * 1024];
        int read;
        try {
            while ((read = in.read(buffer)) != -1) {
                out.write(buffer, 0, read);
            }
            out.flush();
        } finally {
            try { in.close(); } catch (Exception ignored) {}
            try { out.close(); } catch (Exception ignored) {}
        }
    }

    private boolean hasRequiredData(File dir) {
        // The engine currently mounts the A1..AC archive set. A1 is the minimum
        // sanity check; the remaining archives are mounted opportunistically.
        return new File(dir, "A1.dta").exists();
    }

    private void sendResult(final boolean success, final String value) {
        UnityPlayer.UnitySendMessage(
                "AndroidDataPicker",
                success ? "OnFolderImported" : "OnFolderImportFailed",
                value == null ? "" : value);
    }
}
