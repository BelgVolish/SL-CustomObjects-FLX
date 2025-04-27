using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DONT_TOUCH.Scripts.SchematicProtection;
using Newtonsoft.Json;
using UnityEngine;

namespace DONT_TOUCH.Scripts.SchematicProtection
{
    public static class SchematicExporter
    {
        // Структура метаданных для экспорта
        [Serializable]
        private class SchematicMetadata
        {
            public string schematicId;
            public string title;
            public string creator;
            public string creationDate;
            public string lastModified;
            public string version = "1.0.0";
            public string license = "Proprietary";
            public string scpVersion = "Latest";
            public string[] tags;
            public string description;
            public string geometricHash;
            public string contentHash;
            public string[] watermarkData;
        }
        
        // Экспортирует схематик в файл
        public static bool ExportSchematic(SchematicComponent schematic, string filePath)
        {
            try
            {
                // Обновляем геометрический хеш перед экспортом
                GeometricHasher.UpdateGeometricHash(schematic);
                
                // Создаем метаданные
                SchematicMetadata metadata = new SchematicMetadata
                {
                    schematicId = schematic.SchematicId,
                    title = schematic.SchematicName,
                    creator = schematic.CreatorName,
                    creationDate = schematic.CreationDate,
                    lastModified = DateTime.UtcNow.ToString("o"),
                    tags = schematic.Tags,
                    description = schematic.Description,
                    geometricHash = schematic.GetGeometricHash(),
                    watermarkData = schematic.GetWatermarkData()
                };
                
                // Сериализуем схематик в JSON
                string schematicJson = SerializeSchematic(schematic.gameObject);
                
                // Вычисляем хеш содержимого
                metadata.contentHash = CalculateContentHash(schematicJson);
                
                // Сериализуем метаданные
                string metadataJson = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                
                // Объединяем метаданные и содержимое
                string finalContent = $"/* METADATA_BEGIN\n{metadataJson}\nMETADATA_END */\n\n{schematicJson}";
                
                // Записываем в файл
                File.WriteAllText(filePath, finalContent);
                
                // Логируем экспорт
                LogExport(schematic, filePath);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error exporting schematic: {ex.Message}");
                return false;
            }
        }
        
        // Сериализует объект схематика в JSON
        private static string SerializeSchematic(GameObject root)
        {
            // Здесь должна быть ваша логика сериализации схематика
            // Это зависит от формата, который вы используете
            
            // Пример простой сериализации иерархии объектов
            var schematicData = new
            {
                name = root.name,
                position = root.transform.localPosition,
                rotation = root.transform.localEulerAngles,
                scale = root.transform.localScale,
                children = SerializeChildren(root.transform)
            };
            
            return JsonConvert.SerializeObject(schematicData, Formatting.Indented);
        }
        
        // Рекурсивно сериализует дочерние объекты
        private static object[] SerializeChildren(Transform parent)
        {
            object[] children = new object[parent.childCount];
            
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                
                // Пропускаем скрытые объекты водяных знаков
                if (child.name.StartsWith("_wm_"))
                    continue;
                
                children[i] = new
                {
                    name = child.name,
                    position = child.localPosition,
                    rotation = child.localEulerAngles,
                    scale = child.localScale,
                    components = SerializeComponents(child.gameObject),
                    children = SerializeChildren(child)
                };
            }
            
            return children;
        }
        
        // Сериализует компоненты объекта
        private static object[] SerializeComponents(GameObject obj)
        {
            // Здесь должна быть логика сериализации компонентов
            // Это сильно зависит от того, какие компоненты вы хотите экспортировать
            
            // Пример: сериализация MeshFilter и MeshRenderer
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            
            if (meshFilter != null && meshRenderer != null && meshFilter.sharedMesh != null)
            {
                return new object[]
                {
                    new
                    {
                        type = "MeshFilter",
                        meshName = meshFilter.sharedMesh.name
                    },
                    new
                    {
                        type = "MeshRenderer",
                        materialName = meshRenderer.sharedMaterial != null ? meshRenderer.sharedMaterial.name : "None"
                    }
                };
            }
            
            return new object[0];
        }
        
        // Вычисляет хеш содержимого
        private static string CalculateContentHash(string content)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }
                
                return builder.ToString();
            }
        }
        
        // Логирует экспорт схематика
        private static void LogExport(SchematicComponent schematic, string filePath)
        {
            string logMessage = $"Exported schematic: {schematic.SchematicName} (ID: {schematic.SchematicId}) by {schematic.CreatorName} to {filePath}";
            Debug.Log(logMessage);
            
            // Можно добавить отправку этой информации на сервер или в локальный лог
            string logFilePath = Path.Combine(Application.persistentDataPath, "schematic_exports.log");
            string logEntry = $"[{DateTime.Now}] {logMessage}\n";
            
            try
            {
                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to write to export log: {ex.Message}");
            }
        }
    }
}