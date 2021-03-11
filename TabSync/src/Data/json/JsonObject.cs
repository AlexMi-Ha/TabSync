using System;
using System.Collections.Generic;
using System.Text;

namespace TabSync.src.Data.json {
    class JsonObject {
        // Path of the tab file (.tg, .gpX...)
        public String TabPath { get; set; }
        // Path of the mp3 File
        public String SongPath { get; set; }

        // Time where to sync song and tab
        public long SongSyncTime { get; set; }
    }
}
