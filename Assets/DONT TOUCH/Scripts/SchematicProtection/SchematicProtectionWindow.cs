using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DONT_TOUCH.Scripts.SchematicProtection
{
    public class SchematicProtectionWindow : EditorWindow
    {
        // Вкладки
        private enum Tab
        {
            Settings,
            Protection,
            Verification,
            About
        }
        
        // Текущая вкладка
        private Tab currentTab = Tab.Settings;
        
        // Настройки пользователя
        private string creatorName = "";
        private string licenseType = "Proprietary";
        private string[] licenseTypes = new string[] { "Proprietary", "MIT", "Creative Commons", "GPL", "Custom" };
        private string customLicense = "";
        private string reportingEmail = "";
        private bool enableAutoProtection = true;
        private bool enableExportNotification = true;
        
        // Настройки проверки
        private GameObject objectToCheck;
        private GameObject referenceObject;
        private float similarityThreshold = 0.8f;
        
        // Статистика
        private int protectedCount = 0;
        private int unprotectedCount = 0;
        private DateTime lastScan = DateTime.MinValue;
        
        // Стили
        private GUIStyle headerStyle;
        private GUIStyle tabStyle;
        private GUIStyle activeTabStyle;
        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        
        // Текстуры и иконки
        private Texture2D logoTexture;
        private Texture2D settingsIcon;
        private Texture2D protectionIcon;
        private Texture2D verificationIcon;
        private Texture2D infoIcon;
        
        // Открывает окно
        [MenuItem("SCP SL Tools/Schematic Protection")]
        public static void ShowWindow()
        {
            SchematicProtectionWindow window = GetWindow<SchematicProtectionWindow>("Schematic Protection");
            window.minSize = new Vector2(500, 400);
        }
        
        // Инициализация
        private void OnEnable()
        {
            // Загружаем настройки
            LoadSettings();
            
            // Загружаем иконки
            LoadIcons();
            
            // Обновляем статистику
            UpdateStatistics();
        }
        
        // Загружает настройки
        private void LoadSettings()
        {
            creatorName = EditorPrefs.GetString("SchematicProtection_CreatorName", "");
            licenseType = EditorPrefs.GetString("SchematicProtection_LicenseType", "Proprietary");
            customLicense = EditorPrefs.GetString("SchematicProtection_CustomLicense", "");
            reportingEmail = EditorPrefs.GetString("SchematicProtection_ReportingEmail", "");
            enableAutoProtection = EditorPrefs.GetBool("SchematicProtection_AutoProtection", true);
            enableExportNotification = EditorPrefs.GetBool("SchematicProtection_ExportNotification", true);
            similarityThreshold = EditorPrefs.GetFloat("SchematicProtection_SimilarityThreshold", 0.8f);
        }
        
        // Сохраняет настройки
        private void SaveSettings()
        {
            EditorPrefs.SetString("SchematicProtection_CreatorName", creatorName);
            EditorPrefs.SetString("SchematicProtection_LicenseType", licenseType);
            EditorPrefs.SetString("SchematicProtection_CustomLicense", customLicense);
            EditorPrefs.SetString("SchematicProtection_ReportingEmail", reportingEmail);
            EditorPrefs.SetBool("SchematicProtection_AutoProtection", enableAutoProtection);
            EditorPrefs.SetBool("SchematicProtection_ExportNotification", enableExportNotification);
            EditorPrefs.SetFloat("SchematicProtection_SimilarityThreshold", similarityThreshold);
        }
        
        // Загружает иконки
        private void LoadIcons()
        {
            // Здесь должна быть загрузка иконок
            // В реальном плагине вы бы загружали их из Resources
            // Для примера используем стандартные иконки Unity
            
            settingsIcon = EditorGUIUtility.FindTexture("Settings");
            protectionIcon = EditorGUIUtility.FindTexture("Prefab Icon");
            verificationIcon = EditorGUIUtility.FindTexture("FilterByType");
            infoIcon = EditorGUIUtility.FindTexture("console.infoicon");
        }
        
        // Обновляет статистику
        private void UpdateStatistics()
        {
            protectedCount = 0;
            unprotectedCount = 0;
            
            // Находим все схематики в проекте
            SchematicComponent[] schematics = FindObjectsOfType<SchematicComponent>();
            
            foreach (SchematicComponent schematic in schematics)
            {
                bool hasHash = !string.IsNullOrEmpty(schematic.GetGeometricHash());
                bool hasWatermark = schematic.GetWatermarkData() != null && schematic.GetWatermarkData().Length > 0;
                
                if (hasHash && hasWatermark)
                {
                    protectedCount++;
                }
                else
                {
                    unprotectedCount++;
                }
            }
            
            lastScan = DateTime.Now;
        }
        
        // Отрисовка GUI
        private void OnGUI()
        {
            // Инициализация стилей
            InitStyles();
            
            // Отрисовка заголовка
            DrawHeader();
            
            // Отрисовка вкладок
            DrawTabs();
            
            // Отрисовка содержимого текущей вкладки
            EditorGUILayout.BeginVertical(boxStyle);
            
            switch (currentTab)
            {
                case Tab.Settings:
                    DrawSettingsTab();
                    break;
                case Tab.Protection:
                    DrawProtectionTab();
                    break;
                case Tab.Verification:
                    DrawVerificationTab();
                    break;
                case Tab.About:
                    DrawAboutTab();
                    break;
            }
            
            EditorGUILayout.EndVertical();
            
            // Отрисовка футера
            DrawFooter();
        }
        
        // Инициализация стилей
        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.fontSize = 18;
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.alignment = TextAnchor.MiddleCenter;
                headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                
                tabStyle = new GUIStyle(GUI.skin.button);
                tabStyle.fixedHeight = 30;
                tabStyle.margin = new RectOffset(0, 0, 0, 0);
                tabStyle.fontSize = 12;
                
                activeTabStyle = new GUIStyle(tabStyle);
                activeTabStyle.normal.background = activeTabStyle.active.background;
                
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.padding = new RectOffset(10, 10, 10, 10);
                
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 14;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.margin = new RectOffset(0, 0, 10, 10);
            }
        }
        
        // Отрисовка заголовка
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("SCP SL Schematic Protection", headerStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        // Отрисовка вкладок
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(currentTab == Tab.Settings, new GUIContent(" Settings", settingsIcon), 
                currentTab == Tab.Settings ? activeTabStyle : tabStyle, GUILayout.Width(120)))
            {
                currentTab = Tab.Settings;
            }
            
            if (GUILayout.Toggle(currentTab == Tab.Protection, new GUIContent(" Protection", protectionIcon), 
                currentTab == Tab.Protection ? activeTabStyle : tabStyle, GUILayout.Width(120)))
            {
                currentTab = Tab.Protection;
            }
            
            if (GUILayout.Toggle(currentTab == Tab.Verification, new GUIContent(" Verification", verificationIcon), 
                currentTab == Tab.Verification ? activeTabStyle : tabStyle, GUILayout.Width(120)))
            {
                currentTab = Tab.Verification;
            }
            
            if (GUILayout.Toggle(currentTab == Tab.About, new GUIContent(" About", infoIcon), 
                currentTab == Tab.About ? activeTabStyle : tabStyle, GUILayout.Width(120)))
            {
                currentTab = Tab.About;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        // Отрисовка вкладки настроек
        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("User Settings", titleStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Имя создателя
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Creator Name:", GUILayout.Width(120));
            string newCreatorName = EditorGUILayout.TextField(creatorName);
            if (newCreatorName != creatorName)
            {
                creatorName = newCreatorName;
                SaveSettings();
            }
            EditorGUILayout.EndHorizontal();
            
            // Тип лицензии
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("License Type:", GUILayout.Width(120));
            int licenseIndex = Array.IndexOf(licenseTypes, licenseType);
            int newLicenseIndex = EditorGUILayout.Popup(licenseIndex, licenseTypes);
            if (newLicenseIndex != licenseIndex)
            {
                licenseType = licenseTypes[newLicenseIndex];
                SaveSettings();
            }
            EditorGUILayout.EndHorizontal();
            
            // Пользовательская лицензия
            if (licenseType == "Custom")
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Custom License:", GUILayout.Width(120));
                string newCustomLicense = EditorGUILayout.TextField(customLicense);
                if (newCustomLicense != customLicense)
                {
                    customLicense = newCustomLicense;
                    SaveSettings();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // Email для отчетов
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Reporting Email:", GUILayout.Width(120));
            string newReportingEmail = EditorGUILayout.TextField(reportingEmail);
            if (newReportingEmail != reportingEmail)
            {
                reportingEmail = newReportingEmail;
                SaveSettings();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Protection Settings", titleStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Автоматическая защита
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Auto-Protection:", GUILayout.Width(120));
            bool newEnableAutoProtection = EditorGUILayout.Toggle(enableAutoProtection);
            if (newEnableAutoProtection != enableAutoProtection)
            {
                enableAutoProtection = newEnableAutoProtection;
                SaveSettings();
            }
            EditorGUILayout.EndHorizontal();
            
            // Уведомления при экспорте
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Export Notification:", GUILayout.Width(120));
            bool newEnableExportNotification = EditorGUILayout.Toggle(enableExportNotification);
            if (newEnableExportNotification != enableExportNotification)
            {
                enableExportNotification = newEnableExportNotification;
                SaveSettings();
            }
            EditorGUILayout.EndHorizontal();
            
            // Порог схожести
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Similarity Threshold:", GUILayout.Width(120));
            float newSimilarityThreshold = EditorGUILayout.Slider(similarityThreshold, 0.5f, 1.0f);
            if (Math.Abs(newSimilarityThreshold - similarityThreshold) > 0.001f)
            {
                similarityThreshold = newSimilarityThreshold;
                SaveSettings();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Кнопки действий
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Reset Settings", 
                    "Are you sure you want to reset all settings to default values?", 
                    "Reset", "Cancel"))
                {
                    ResetSettings();
                }
            }
            
            if (GUILayout.Button("Apply to All Schematics", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Apply Settings", 
                    "This will apply current settings to all schematics in the scene. Continue?", 
                    "Apply", "Cancel"))
                {
                    ApplySettingsToAllSchematics();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        // Отрисовка вкладки защиты
        private void DrawProtectionTab()
        {
            EditorGUILayout.LabelField("Protection Status", titleStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Статистика
            EditorGUILayout.LabelField($"Protected Schematics: {protectedCount}");
            EditorGUILayout.LabelField($"Unprotected Schematics: {unprotectedCount}");
            EditorGUILayout.LabelField($"Last Scan: {(lastScan == DateTime.MinValue ? "Never" : lastScan.ToString())}");
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Refresh Statistics"))
            {
                UpdateStatistics();
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Batch Operations", titleStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Кнопки для массовых операций
            if (GUILayout.Button("Protect All Unprotected Schematics", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Protect All", 
                    "This will apply protection to all unprotected schematics in the scene. Continue?", 
                    "Protect All", "Cancel"))
                {
                    ProtectAllSchematics();
                }
            }
            
            if (GUILayout.Button("Update Protection for All Schematics", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Update All", 
                    "This will update protection for all schematics in the scene. Continue?", 
                    "Update All", "Cancel"))
                {
                    UpdateAllSchematics();
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Export Operations", titleStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Кнопки для экспорта
       
            if (GUILayout.Button("Export Selected Schematic", GUILayout.Height(30)))
            {
                ExportSelectedSchematic();
            }
            
            if (GUILayout.Button("Export All Schematics", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Export All", 
                    "This will export all schematics in the scene to individual files. Continue?", 
                    "Export All", "Cancel"))
                {
                    ExportAllSchematics();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Отрисовка вкладки проверки
        private void DrawVerificationTab()
        {
            EditorGUILayout.LabelField("Schematic Verification", titleStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Выбор объекта для проверки
            EditorGUILayout.LabelField("Verify a schematic against a reference or detect plagiarism");
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Schematic to Check:", GUILayout.Width(150));
            objectToCheck = (GameObject)EditorGUILayout.ObjectField(objectToCheck, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Кнопки проверки
            if (GUILayout.Button("Verify Integrity", GUILayout.Height(30)))
            {
                VerifySchematicIntegrity();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Plagiarism Detection", titleStyle);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Reference Schematic:", GUILayout.Width(150));
            referenceObject = (GameObject)EditorGUILayout.ObjectField(referenceObject, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Порог схожести
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Similarity Threshold:", GUILayout.Width(150));
            similarityThreshold = EditorGUILayout.Slider(similarityThreshold, 0.5f, 1.0f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Compare Schematics", GUILayout.Height(30)))
            {
                CompareSchematics();
            }
            
            if (GUILayout.Button("Scan Project for Similar Schematics", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Scan Project", 
                    "This will scan the entire project for similar schematics. This may take some time. Continue?", 
                    "Scan", "Cancel"))
                {
                    ScanProjectForSimilarSchematics();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Отрисовка вкладки "О плагине"
        private void DrawAboutTab()
        {
            EditorGUILayout.LabelField("About SCP SL Schematic Protection", titleStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Version: 1.0.0");
            EditorGUILayout.LabelField("Created for: SCP SL");
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("This plugin provides protection for SCP SL schematics by embedding metadata, " +
                "geometric hashing, and watermarking. It helps prevent unauthorized use of your schematics and " +
                "allows detection of plagiarism.", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Features:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Automatic metadata embedding", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Geometric hashing for structure verification", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Invisible watermarking", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Plagiarism detection", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Export protection", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("How to use:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Add the Schematic component to your root object", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("2. Set your creator name in the Settings tab", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("3. Use the Protection tab to apply protection", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("4. Export your schematic using the provided tools", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Open Documentation", GUILayout.Height(30)))
            {
                OpenDocumentation();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Отрисовка футера
        private void DrawFooter()
        {
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("© 2023 SCP SL Schematic Protection", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }
        
        // Сбрасывает настройки на значения по умолчанию
        private void ResetSettings()
        {
            creatorName = "";
            licenseType = "Proprietary";
            customLicense = "";
            reportingEmail = "";
            enableAutoProtection = true;
            enableExportNotification = true;
            similarityThreshold = 0.8f;
            
            SaveSettings();
        }
        
        // Применяет настройки ко всем схематикам
        private void ApplySettingsToAllSchematics()
        {
            SchematicComponent[] schematics = FindObjectsOfType<SchematicComponent>();
            int count = 0;
            
            foreach (SchematicComponent schematic in schematics)
            {
                // Обновляем информацию о создателе
                SerializedObject serializedObject = new SerializedObject(schematic);
                SerializedProperty creatorProperty = serializedObject.FindProperty("creatorName");
                
                if (creatorProperty != null && creatorProperty.stringValue != creatorName)
                {
                    creatorProperty.stringValue = creatorName;
                    serializedObject.ApplyModifiedProperties();
                    count++;
                }
            }
            
            EditorUtility.DisplayDialog("Settings Applied", 
                $"Settings have been applied to {count} schematics.", "OK");
        }
        
        // Защищает все незащищенные схематики
        private void ProtectAllSchematics()
        {
            SchematicComponent[] schematics = FindObjectsOfType<SchematicComponent>();
            int count = 0;
            
            foreach (SchematicComponent schematic in schematics)
            {
                bool hasHash = !string.IsNullOrEmpty(schematic.GetGeometricHash());
                bool hasWatermark = schematic.GetWatermarkData() != null && schematic.GetWatermarkData().Length > 0;
                
                if (!hasHash || !hasWatermark)
                {
                    // Обновляем геометрический хеш
                    GeometricHasher.UpdateGeometricHash(schematic);
                    
                    // Применяем водяной знак, если его нет
                    if (!hasWatermark)
                    {
                        WatermarkGenerator.ApplyWatermark(schematic);
                    }
                    
                    // Обновляем представление в инспекторе
                    EditorUtility.SetDirty(schematic);
                    
                    count++;
                }
            }
            
            // Обновляем статистику
            UpdateStatistics();
            
            EditorUtility.DisplayDialog("Protection Applied", 
                $"Protection has been applied to {count} schematics.", "OK");
        }
        
        // Обновляет защиту всех схематиков
        private void UpdateAllSchematics()
        {
            SchematicComponent[] schematics = FindObjectsOfType<SchematicComponent>();
            int count = 0;
            
            foreach (SchematicComponent schematic in schematics)
            {
                // Обновляем геометрический хеш
                GeometricHasher.UpdateGeometricHash(schematic);
                
                // Обновляем представление в инспекторе
                EditorUtility.SetDirty(schematic);
                
                count++;
            }
            
            // Обновляем статистику
            UpdateStatistics();
            
            EditorUtility.DisplayDialog("Protection Updated", 
                $"Protection has been updated for {count} schematics.", "OK");
        }
        
        // Экспортирует выбранный схематик
        private void ExportSelectedSchematic()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No Selection", 
                    "Please select a GameObject with a Schematic component.", "OK");
                return;
            }
            
            SchematicComponent schematic = Selection.activeGameObject.GetComponent<SchematicComponent>();
            
            if (schematic == null)
            {
                EditorUtility.DisplayDialog("Invalid Selection", 
                    "The selected GameObject does not have a Schematic component.", "OK");
                return;
            }
            
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
        
        // Экспортирует все схематики
        private void ExportAllSchematics()
        {
            SchematicComponent[] schematics = FindObjectsOfType<SchematicComponent>();
            
            if (schematics.Length == 0)
            {
                EditorUtility.DisplayDialog("No Schematics", 
                    "No schematics found in the scene.", "OK");
                return;
            }
            
            // Выбираем директорию для экспорта
            string directory = EditorUtility.SaveFolderPanel(
                "Select Export Directory",
                "",
                ""
            );
            
            if (string.IsNullOrEmpty(directory))
                return;
            
            int successCount = 0;
            int failCount = 0;
            
            foreach (SchematicComponent schematic in schematics)
            {
                // Обновляем защиту перед экспортом
                GeometricHasher.UpdateGeometricHash(schematic);
                
                // Формируем имя файла
                string fileName = schematic.SchematicName.Replace(" ", "_").ToLower() + ".schematic";
                string path = Path.Combine(directory, fileName);
                
                // Экспортируем схематик
                bool success = SchematicExporter.ExportSchematic(schematic, path);
                
                if (success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }
            
            EditorUtility.DisplayDialog("Export Complete", 
                $"Successfully exported {successCount} schematics.\nFailed to export {failCount} schematics.\n\nExport directory: {directory}", "OK");
        }
        
        // Проверяет целостность схематика
        private void VerifySchematicIntegrity()
        {
            if (objectToCheck == null)
            {
                EditorUtility.DisplayDialog("No Schematic Selected", 
                    "Please select a schematic to check.", "OK");
                return;
            }
            
            SchematicComponent schematic = objectToCheck.GetComponent<SchematicComponent>();
            
            if (schematic == null)
            {
                EditorUtility.DisplayDialog("Invalid Selection", 
                    "The selected GameObject does not have a Schematic component.", "OK");
                return;
            }
            
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
        
        // Сравнивает два схематика
        private void CompareSchematics()
        {
            if (objectToCheck == null || referenceObject == null)
            {
                EditorUtility.DisplayDialog("Missing Selection", 
                    "Please select both a schematic to check and a reference schematic.", "OK");
                return;
            }
            
            SchematicComponent schematic1 = objectToCheck.GetComponent<SchematicComponent>();
            SchematicComponent schematic2 = referenceObject.GetComponent<SchematicComponent>();
            
            if (schematic1 == null || schematic2 == null)
            {
                EditorUtility.DisplayDialog("Invalid Selection", 
                    "One or both of the selected GameObjects do not have a Schematic component.", "OK");
                return;
            }
            
            // Сравниваем геометрические хеши
            float geometricSimilarity = GeometricHasher.CompareGeometricHashes(objectToCheck, referenceObject);
            
            // Сравниваем водяные знаки
            float watermarkSimilarity = WatermarkGenerator.CompareWatermarks(schematic1, schematic2);
            
            // Вычисляем общую схожесть
            float overallSimilarity = (geometricSimilarity + watermarkSimilarity) / 2;
            
            // Формируем отчет
            StringBuilder report = new StringBuilder();
            report.AppendLine($"Comparison Report");
            report.AppendLine($"Schematic 1: {schematic1.SchematicName} (Creator: {schematic1.CreatorName})");
            report.AppendLine($"Schematic 2: {schematic2.SchematicName} (Creator: {schematic2.CreatorName})");
            report.AppendLine();
            report.AppendLine($"Geometric Similarity: {geometricSimilarity:P2}");
            report.AppendLine($"Watermark Similarity: {watermarkSimilarity:P2}");
            report.AppendLine($"Overall Similarity: {overallSimilarity:P2}");
            report.AppendLine();
            
            if (overallSimilarity >= similarityThreshold)
            {
                report.AppendLine("RESULT: SIMILAR");
                report.AppendLine($"The schematics are similar above the threshold of {similarityThreshold:P2}.");
                
                if (schematic1.CreatorName != schematic2.CreatorName)
                {
                    report.AppendLine();
                    report.AppendLine("WARNING: Different Creators");
                    report.AppendLine("These similar schematics have different creators, which may indicate plagiarism.");
                }
            }
            else
            {
                report.AppendLine("RESULT: DIFFERENT");
                report.AppendLine($"The schematics are different below the threshold of {similarityThreshold:P2}.");
            }
            
            // Показываем отчет
            EditorUtility.DisplayDialog("Schematic Comparison", report.ToString(), "OK");
        }
        
        // Сканирует проект на наличие похожих схематиков
        private void ScanProjectForSimilarSchematics()
        {
            if (objectToCheck == null)
            {
                EditorUtility.DisplayDialog("No Schematic Selected", 
                    "Please select a schematic to check against the project.", "OK");
                return;
            }
            
            SchematicComponent targetSchematic = objectToCheck.GetComponent<SchematicComponent>();
            
            if (targetSchematic == null)
            {
                EditorUtility.DisplayDialog("Invalid Selection", 
                    "The selected GameObject does not have a Schematic component.", "OK");
                return;
            }
            
            // Находим все схематики в сцене
            SchematicComponent[] schematics = FindObjectsOfType<SchematicComponent>();
            
            // Список похожих схематиков
            List<KeyValuePair<SchematicComponent, float>> similarSchematics = new List<KeyValuePair<SchematicComponent, float>>();
            
            // Сравниваем с каждым схематиком
            foreach (SchematicComponent schematic in schematics)
            {
                // Пропускаем сам схематик
                if (schematic == targetSchematic)
                    continue;
                
                // Сравниваем геометрические хеши
                float geometricSimilarity = GeometricHasher.CompareGeometricHashes(objectToCheck, schematic.gameObject);
                
                // Сравниваем водяные знаки
                float watermarkSimilarity = WatermarkGenerator.CompareWatermarks(targetSchematic, schematic);
                
                // Вычисляем общую схожесть
                float overallSimilarity = (geometricSimilarity + watermarkSimilarity) / 2;
                
                // Если схожесть выше порога, добавляем в список
                if (overallSimilarity >= similarityThreshold)
                {
                    similarSchematics.Add(new KeyValuePair<SchematicComponent, float>(schematic, overallSimilarity));
                }
            }
            
            // Сортируем по убыванию схожести
            similarSchematics.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            // Формируем отчет
            StringBuilder report = new StringBuilder();
            report.AppendLine($"Scan Results for: {targetSchematic.SchematicName}");
            report.AppendLine($"Creator: {targetSchematic.CreatorName}");
            report.AppendLine($"Similarity Threshold: {similarityThreshold:P2}");
            report.AppendLine();
            
            if (similarSchematics.Count > 0)
            {
                report.AppendLine($"Found {similarSchematics.Count} similar schematics:");
                report.AppendLine();
                
                for (int i = 0; i < similarSchematics.Count; i++)
                {
                    SchematicComponent schematic = similarSchematics[i].Key;
                    float similarity = similarSchematics[i].Value;
                    
                    report.AppendLine($"{i+1}. {schematic.SchematicName} (Similarity: {similarity:P2})");
                    report.AppendLine($"   Creator: {schematic.CreatorName}");
                    report.AppendLine($"   ID: {schematic.SchematicId}");
                    
                    if (schematic.CreatorName != targetSchematic.CreatorName)
                    {
                        report.AppendLine($"   WARNING: Different creator!");
                    }
                    
                    report.AppendLine();
                }
            }
            else
            {
                report.AppendLine("No similar schematics found in the scene.");
            }
            
            // Показываем отчет
            EditorUtility.DisplayDialog("Scan Results", report.ToString(), "OK");
        }
        
        // Открывает документацию
        private void OpenDocumentation()
        {
            // Путь к документации
            string documentationPath = "Assets/SchematicProtection/Documentation/UserGuide.md";
            
            // Проверяем, существует ли файл
            if (File.Exists(documentationPath))
            {
                // Открываем файл
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(documentationPath, 1);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation Not Found", 
                    "The documentation file could not be found.", "OK");
            }
        }
    }
}
