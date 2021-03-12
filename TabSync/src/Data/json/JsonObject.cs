using System;

namespace TabSync.src.Data.json {
    public class JsonObject {
        // Path of the tab file (.tg, .gpX...)
        public String TabPath { get; set; }
        // Path of the mp3 File
        public String SongPath { get; set; }

        // Time where to sync song and tab
        public long SongPlaybackOffset { get; set; }

    }
}
