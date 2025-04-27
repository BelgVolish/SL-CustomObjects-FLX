using System;
using UnityEngine;

namespace DONT_TOUCH.Scripts.SchematicProtection
{
    [AddComponentMenu("SCP SL/Schematic")]
    public class SchematicComponent : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string schematicId;
        [SerializeField, HideInInspector] private string creatorName;
        [SerializeField, HideInInspector] private string creationDate;
        [SerializeField, HideInInspector] private string geometricHash;
        [SerializeField, HideInInspector] private string[] watermarkData;
        
        [SerializeField] public string schematicName = "New Schematic";
        [SerializeField] public string description = "";
        [SerializeField] public string[] tags = new string[0];
        
        // Эти поля видны в инспекторе
        public string SchematicName => schematicName;
        public string Description => description;
        public string[] Tags => tags;
        
        // Эти поля скрыты от обычного просмотра
        public string SchematicId => schematicId;
        public string CreatorName => creatorName;
        public string CreationDate => creationDate;
        
        private void OnValidate()
        {
            // Генерируем ID для новых схематиков
            if (string.IsNullOrEmpty(schematicId))
            {
                schematicId = Guid.NewGuid().ToString();
                creationDate = DateTime.UtcNow.ToString("o");
                
                // Получаем имя создателя из настроек плагина (только в редакторе)
                #if UNITY_EDITOR
                creatorName = UnityEditor.EditorPrefs.GetString("SchematicProtection_CreatorName", "Unknown");
                #endif
            }
        }
        
        // Вызывается при добавлении компонента
        private void Reset()
        {
            schematicId = Guid.NewGuid().ToString();
            creationDate = DateTime.UtcNow.ToString("o");
            
            #if UNITY_EDITOR
            creatorName = UnityEditor.EditorPrefs.GetString("SchematicProtection_CreatorName", "Unknown");
            
            // Автоматически обновляем геометрический хеш и водяные знаки
            UnityEditor.EditorApplication.delayCall += () => {
                if (this != null)
                {
                    GeometricHasher.UpdateGeometricHash(this);
                    WatermarkGenerator.ApplyWatermark(this);
                }
            };
            #endif
        }
        
        public void SetGeometricHash(string hash)
        {
            geometricHash = hash;
        }
        
        public string GetGeometricHash()
        {
            return geometricHash;
        }
        
        public void SetWatermarkData(string[] data)
        {
            watermarkData = data;
        }
        
        public string[] GetWatermarkData()
        {
            return watermarkData;
        }
    }
}