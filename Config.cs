using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace MusicBeePlugin
{
    public class ConfigBase
    {
        /// <summary>
        /// デフォルト値を読み込みます。
        /// </summary>
        public void LoadDefault()
        {
            var type = this.GetType();

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo prop in properties)
            {
                // 各プロパティに[Default***]を代入する
                FieldInfo defaultField = type.GetField("Default" + prop.Name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (defaultField != null)
                {
                    object defaultValue = defaultField.GetValue(this);
                    prop.SetValue(this, defaultValue, null);
                }
            }
        }

        /// <summary>
        /// 指定されたパスに設定をXMLシリアライズして保存します。
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            var serializer = new XmlSerializer(this.GetType());
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                serializer.Serialize(fs, this);
            }
        }

        /// <summary>
        /// 指定されたパスからXMLを取得し、デシリアライズして設定を読み込みます。
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            if (!File.Exists(path))
            {
                this.LoadDefault();
                return;
            }

            var serializer = new XmlSerializer(this.GetType());
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    var config = (ConfigBase)serializer.Deserialize(fs);

                    PropertyInfo[] properties = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    foreach (PropertyInfo prop in properties)
                    {
                        object newValue = prop.GetValue(config, null);
                        prop.SetValue(this, newValue, null);
                    }
                }
                catch (InvalidOperationException)
                {
                    this.LoadDefault();
                }
            }
        }
    }

    [Serializable]
    public class Config
        : ConfigBase
    {
        private Config() { }
        private static Config instance = new Config();
        public  static Config Instance { get { return Config.instance; } }
        
        public readonly int    DefaultFadeOutTimeMills = 3000;
        
        public int    FadeOutTimeMills { get; set; } = 3000;
    }
}