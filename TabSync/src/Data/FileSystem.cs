using System;
using System.IO;
using Newtonsoft.Json;

using TabSync.src.Data.json;

namespace TabSync.src.Data {
    public class FileSystem {

        private JsonObject jsonContent; //content of the Json file saved as a custom Object

        private string filename;    //path of the save file json

        public FileSystem(string _filename) {
            Reload(_filename);
        }

        //Default method for Saving the data
        public void Save() {
            Save(this.filename);
        }
        public void Save(string filename) {
            this.filename = filename;
            if (!File.Exists(filename)) {
                File.Create(filename).Close();
            }
            string jsonData = JsonConvert.SerializeObject(jsonContent); //Convert the JsonObject into a String with Json data

            using (StreamWriter file = new StreamWriter(filename)) {
                file.WriteLine(jsonData);
            }
        }

        //Reloads the File
        public void Reload() {
            Reload(this.filename);
        }

        public void Reload(string filename) {
            this.filename = filename;
            jsonContent = null;

            if (File.Exists(filename)) {
                LoadFromFile(filename);
            } else {
                File.Create(filename).Close();
                jsonContent = new JsonObject();
            }
        }

        //Load all Data in the Json file and put it in the JsonObject
        private void LoadFromFile(string file) {
            string json = String.Join(" ", File.ReadAllLines(file));
            if (string.IsNullOrWhiteSpace(json)) {
                jsonContent = new JsonObject();
                return;
            }
            jsonContent = JsonConvert.DeserializeObject<JsonObject>(json);


        }

        public JsonObject GetJsonObject() {
            return jsonContent;
        }

    }
}
