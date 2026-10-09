using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace CannibalRally
{
    public static class SidecarStore
    {
        public static T Read<T>(string path)
        {
            if (new FileInfo(path).Length > 65536) throw new InvalidOperationException("XML file too large.");
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = true;
            settings.XmlResolver = null;
            using (XmlReader reader = XmlReader.Create(path, settings))
                return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
        }

        public static void Write<T>(string path, T value)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(path));
            Directory.CreateDirectory(directory);
            string temp = path + ".tmp";
            try
            {
                using (FileStream stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    new XmlSerializer(typeof(T)).Serialize(stream, value);
                    stream.Flush();
                }
                // Never delete the previous good save before replacing it.
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        public static RallyState Load(string path, RallyConfig config)
        {
            RallyState state = Read<RallyState>(path);
            state.Validate(config);
            return state;
        }
    }
}
