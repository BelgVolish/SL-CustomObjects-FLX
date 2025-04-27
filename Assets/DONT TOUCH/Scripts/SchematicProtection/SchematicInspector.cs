using UnityEngine;
using UnityEditor;
using System;
using System.Text;

namespace DONT_TOUCH.Scripts.SchematicProtection
{
    [CustomEditor(typeof(SchematicComponent))]
    public class SchematicInspector : Editor
    {
        private SerializedProperty schematicNameProperty;
        private SerializedProperty descriptionProperty;
        private SerializedProperty tagsProperty;
        
        private bool showAdvanced = false;
        private bool showProtection = false;
        
        private GUIStyle headerStyle;
        private GUIStyle infoStyle;
        
        private void OnEnable()
        {
            schematicNameProperty = serializedObject.FindProperty("schematicName");
            descriptionProperty = serializedObject.FindProperty("description");
            tagsProperty = serializedObject.FindProperty("tags");
        }
        
        // Показывает полный хеш
        private void ShowFullHash(SchematicComponent schematic)
        {
            string hash = schematic.GetGeometricHash();
            
            if (string.IsNullOrEmpty(hash))
            {
                EditorUtility.DisplayDialog("No Hash", 
                    "This schematic doesn't have a geometric hash yet. Click 'Update Protection' to generate one.", 
                    "OK");
                return;
            }
            
            // Создаем окно для отображения хеша
            SchematicHashViewer.ShowWindow(schematic.SchematicName, hash);
        }
        
        // Обновляет защиту схематика
        private void UpdateSchematicProtection(SchematicComponent schematic)
        {
            // Обновляем геометрический хеш
            GeometricHasher.UpdateGeometricHash(schematic);
            
            // Проверяем, есть ли уже водяной знак
            bool hasWatermark = schematic.GetWatermarkData() != null && schematic.GetWatermarkData().Length > 0;
            if (!hasWatermark)
            {
                // Применяем водяной знак
                WatermarkGenerator.ApplyWatermark(schematic);
            }
            
            // Обновляем представление в инспекторе
            EditorUtility.SetDirty(schematic);
            
            // Показываем сообщение об успехе
            EditorUtility.DisplayDialog("Protection Updated", 
                "The schematic protection has been successfully updated.", "OK");
        }
        
        // Пересчитывает геометрический хеш
        private void RecalculateGeometricHash(SchematicComponent schematic)
        {
            // Обновляем геометрический хеш
            GeometricHasher.UpdateGeometricHash(schematic);
            
            // Обновляем представление в инспекторе
            EditorUtility.SetDirty(schematic);
            
            // Показываем сообщение об успехе
            EditorUtility.DisplayDialog("Hash Recalculated", 
                "The geometric hash has been recalculated.", "OK");
        }
        
        private void ExportSchematic(SchematicComponent schematic)
        {
            // Обновляем защиту перед экспортом
            GeometricHasher.UpdateGeometricHash(schematic);
            
            // Открываем диалог сохранения файла
            string defaultName = schematic.SchematicName.Replace(" ", "_").ToLower() + ".schematic";
            string path = EditorUtility.SaveFilePanel(
                "Export Schematic",
                "",
                defaultName,
                "schematic"
            );
            
            if (string.IsNullOrEmpty(path))
                return;
            
            // Экспортируем схематик
            bool success = SchematicExporter.ExportSchematic(schematic, path);
            
            if (success)
            {
                EditorUtility.DisplayDialog("Export Successful", 
                    $"Schematic '{schematic.SchematicName}' has been exported to:\n{path}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Export Failed", 
                    "Failed to export the schematic. Check the console for details.", "OK");
            }
        }
        
        // Проверяет целостность схематика
        private void VerifySchematicIntegrity(SchematicComponent schematic)
        {
            // Проверяем геометрический хеш
            bool isGeometricHashValid = GeometricHasher.VerifyGeometricHash(schematic);
            
            // Проверяем водяной знак
            bool isWatermarkValid = WatermarkGenerator.VerifyWatermark(schematic);
            
            // Формируем отчет
            StringBuilder report = new StringBuilder();
            report.AppendLine($"Integrity Report for: {schematic.SchematicName}");
            report.AppendLine($"Creator: {schematic.CreatorName}");
            report.AppendLine($"ID: {schematic.SchematicId}");
            report.AppendLine();
            report.AppendLine($"Geometric Hash: {(isGeometricHashValid ? "VALID" : "INVALID")}");
            report.AppendLine($"Watermark: {(isWatermarkValid ? "VALID" : "NOT FOUND OR INVALID")}");
            report.AppendLine();
            
            if (isGeometricHashValid && isWatermarkValid)
            {
                report.AppendLine("Overall Status: INTACT");
                report.AppendLine("This schematic has not been modified since protection was applied.");
            }
            else
            {
                report.AppendLine("Overall Status: MODIFIED");
                report.AppendLine("This schematic has been modified since protection was applied.");
                report.AppendLine("Consider updating the protection to reflect these changes.");
            }
            
            // Показываем отчет
            EditorUtility.DisplayDialog("Integrity Verification", report.ToString(), "OK");
        }
        
        // Регенерирует водяной знак
        private void RegenerateWatermark(SchematicComponent schematic)
        {
            // Удаляем существующий водяной знак
            Transform existingWatermark = schematic.transform.Find("_wm_" + schematic.SchematicId.Substring(0, 8));
            if (existingWatermark != null)
            {
                DestroyImmediate(existingWatermark.gameObject);
            }
            
            // Применяем новый водяной знак
            WatermarkGenerator.ApplyWatermark(schematic);
            
            // Обновляем представление в инспекторе
            EditorUtility.SetDirty(schematic);
            
            // Показываем сообщение об успехе
            EditorUtility.DisplayDialog("Watermark Regenerated", 
                "A new watermark has been applied to the schematic.", "OK");
        }
        
        // Показывает данные водяного знака
        private void ShowWatermarkData(SchematicComponent schematic)
        {
            string[] watermarkData = schematic.GetWatermarkData();
            
            if (watermarkData == null || watermarkData.Length == 0)
            {
                EditorUtility.DisplayDialog("No Watermark", 
                    "This schematic doesn't have a watermark yet. Click 'Update Protection' to apply one.", 
                    "OK");
                return;
            }
            
            // Создаем окно для отображения данных водяного знака
            SchematicWatermarkViewer.ShowWindow(schematic.SchematicName, watermarkData);
        }
        
        // Экспортирует только метаданные
        private void ExportMetadataOnly(SchematicComponent schematic)
        {
            // Открываем диалог сохранения файла
            string defaultName = schematic.SchematicName.Replace(" ", "_").ToLower() + "_metadata.json";
            string path = EditorUtility.SaveFilePanel(
                "Export Metadata",
                "",
                defaultName,
                "json"
            );
            
            if (string.IsNullOrEmpty(path))
                return;
            
            try
            {
                // Создаем метаданные
                var metadata = new
                {
                    schematicId = schematic.SchematicId,
                    title = schematic.SchematicName,
                    creator = schematic.CreatorName,
                    creationDate = schematic.CreationDate,
                    lastModified = DateTime.UtcNow.ToString("o"),
                    description = schematic.Description,
                    tags = schematic.Tags,
                    geometricHash = schematic.GetGeometricHash()
                };
                
                // Сериализуем метаданные
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(metadata, Newtonsoft.Json.Formatting.Indented);
                
                // Записываем в файл
                System.IO.File.WriteAllText(path, json);
                
                EditorUtility.DisplayDialog("Export Successful", 
                    $"Metadata for '{schematic.SchematicName}' has been exported to:\n{path}", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Export Failed", 
                    $"Failed to export metadata: {ex.Message}", "OK");
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Инициализация стилей
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.fontSize = 14;
                
                infoStyle = new GUIStyle(EditorStyles.helpBox);
                infoStyle.normal.textColor = Color.white;
                infoStyle.fontSize = 12;
                infoStyle.padding = new RectOffset(10, 10, 10, 10);
            }
            
            SchematicComponent schematic = (SchematicComponent)target;
            
            // Заголовок
            EditorGUILayout.LabelField("SCP SL Schematic", headerStyle);
            EditorGUILayout.Space();
            
            // Основные настройки
            EditorGUILayout.PropertyField(schematicNameProperty, new GUIContent("Schematic Name"));
            EditorGUILayout.PropertyField(descriptionProperty, new GUIContent("Description"));
            
            // Теги
            EditorGUILayout.PropertyField(tagsProperty, new GUIContent("Tags"), true);
            
            EditorGUILayout.Space();
            
            // Информация о защите
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            showProtection = EditorGUILayout.Foldout(showProtection, "Protection Info", true);
            if (showProtection)
            {
                EditorGUI.indentLevel++;
                
                // Основная информация
                EditorGUILayout.LabelField("Creator:", schematic.CreatorName);
                EditorGUILayout.LabelField("Created:", DateTime.Parse(schematic.CreationDate).ToString("yyyy-MM-dd HH:mm:ss"));
                EditorGUILayout.LabelField("ID:", schematic.SchematicId);
                
                // Информация о хеше
                bool isHashValid = !string.IsNullOrEmpty(schematic.GetGeometricHash());
                
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Geometric Hash:");
                
                if (isHashValid)
                {
                    EditorGUILayout.LabelField("Valid", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("Not Set", EditorStyles.boldLabel);
                }
                EditorGUILayout.EndHorizontal();
                
                // Информация о водяном знаке
                bool hasWatermark = schematic.GetWatermarkData() != null && schematic.GetWatermarkData().Length > 0;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Watermark:");
                
                if (hasWatermark)
                {
                    EditorGUILayout.LabelField("Applied", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("Not Applied", EditorStyles.boldLabel);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
                
                serializedObject.ApplyModifiedProperties();
            
                // Предупреждения и информация
                EditorGUILayout.Space();
                if (!isHashValid || !hasWatermark)
                {
                    EditorGUILayout.HelpBox("This schematic is not fully protected. Click 'Update Protection' to apply full protection.", MessageType.Warning);
                }
            
                // Информация о последних изменениях
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Remember to update protection after making significant changes to the schematic.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Кнопки действий
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Update Protection"))
            {
                // Обновляем защиту
                UpdateSchematicProtection(schematic);
            }
            
            if (GUILayout.Button("Export Schematic"))
            {
                // Экспортируем схематик
                ExportSchematic(schematic);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Кнопка проверки
            if (GUILayout.Button("Verify Integrity"))
            {
                // Проверяем целостность схематика
                VerifySchematicIntegrity(schematic);
            }
            
                           // Расширенные настройки
                EditorGUILayout.Space();
                showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Settings", true);
                if (showAdvanced)
                {
                    EditorGUI.indentLevel++;
                    
                    // Дополнительные настройки защиты
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    // Настройки водяного знака
                    EditorGUILayout.LabelField("Watermark Settings", EditorStyles.boldLabel);
                    
                    if (GUILayout.Button("Regenerate Watermark"))
                    {
                        if (EditorUtility.DisplayDialog("Regenerate Watermark", 
                            "This will create a new watermark pattern for this schematic. Continue?", 
                            "Yes", "Cancel"))
                        {
                            RegenerateWatermark(schematic);
                        }
                    }
                    
                    // Настройки геометрического хеша
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Geometric Hash Settings", EditorStyles.boldLabel);
                    
                    if (GUILayout.Button("Recalculate Geometric Hash"))
                    {
                        if (EditorUtility.DisplayDialog("Recalculate Geometric Hash", 
                            "This will update the geometric hash of this schematic. Continue?", 
                            "Yes", "Cancel"))
                        {
                            RecalculateGeometricHash(schematic);
                        }
                    }
                    
                    // Опции экспорта
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
                    
                    if (GUILayout.Button("Export Metadata Only"))
                    {
                        ExportMetadataOnly(schematic);
                    }
                    
                    EditorGUILayout.EndVertical();
                    
                    // Отладочная информация
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Debug Information", EditorStyles.boldLabel);
                    
                    if (GUILayout.Button("Show Full Hash"))
                    {
                        ShowFullHash(schematic);
                    }
                    
                    if (GUILayout.Button("Show Watermark Data"))
                    {
                        ShowWatermarkData(schematic);
                    }
                    
                    EditorGUI.indentLevel--;
                    
                   
                }
            }
            
           
        }
}
    
    // Окно для отображения хеша
    public class SchematicHashViewer : EditorWindow
    {
        private string schematicName;
        private string hash;
        private Vector2 scrollPosition;
        
        public static void ShowWindow(string name, string hash)
        {
            SchematicHashViewer window = GetWindow<SchematicHashViewer>(true, "Geometric Hash");
            window.schematicName = name;
            window.hash = hash;
            window.minSize = new Vector2(400, 200);
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField($"Geometric Hash for: {schematicName}", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.TextArea(hash, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Copy to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = hash;
                EditorUtility.DisplayDialog("Copied", "Hash has been copied to clipboard.", "OK");
            }
        }
    }
    
    // Окно для отображения данных водяного знака
    public class SchematicWatermarkViewer : EditorWindow
    {
        private string schematicName;
        private string[] watermarkData;
        private Vector2 scrollPosition;
        
        public static void ShowWindow(string name, string[] data)
        {
            SchematicWatermarkViewer window = GetWindow<SchematicWatermarkViewer>(true, "Watermark Data");
            window.schematicName = name;
            window.watermarkData = data;
            window.minSize = new Vector2(400, 300);
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField($"Watermark Data for: {schematicName}", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField($"Total Watermark Elements: {watermarkData.Length}", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            for (int i = 0; i < watermarkData.Length; i++)
            {
                EditorGUILayout.LabelField($"Element {i+1}:", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(watermarkData[i], EditorStyles.textField);
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
        }
    }