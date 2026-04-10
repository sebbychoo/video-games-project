mergeInto(LibraryManager.library, {
    JS_FileSystem_Sync: function () {
        try {
            FS.syncfs(false, function (err) {
                if (err) {
                    console.warn("IndexedDB sync error: " + err);
                }
            });
        } catch (e) {
            console.warn("FS.syncfs not available: " + e);
        }
    }
});
