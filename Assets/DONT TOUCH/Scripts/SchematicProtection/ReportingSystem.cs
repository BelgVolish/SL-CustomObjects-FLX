using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DONT_TOUCH.Scripts.SchematicProtection
{
    public static class ReportingSystem
    {
        // Константа с вашим email-адресом
        private const string AdminEmail = "tlpanari@gmail.com";
        
        // Структура для отчета о нарушении
        [Serializable]
        private class ViolationReport
        {
            public string reporterName;
            public string reporterEmail;
            public string originalSchematicId;
            public string originalSchematicName;
            public string originalCreator;
            public string violationDescription;
            public string violatorName;
            public string evidenceDescription;
            public DateTime reportDate;
        }
        
        // Отправляет отчет о нарушении
        public static bool ReportViolation(SchematicComponent originalSchematic, string violatorName, string description, string evidenceDescription)
        {
            try
            {
                // Получаем информацию о создателе из настроек
                string reporterName = EditorPrefs.GetString("SchematicProtection_CreatorName", "Unknown");
                string reporterEmail = EditorPrefs.GetString("SchematicProtection_ReportingEmail", "");
                
                // Создаем отчет
                ViolationReport report = new ViolationReport
                {
                    reporterName = reporterName,
                    reporterEmail = reporterEmail,
                    originalSchematicId = originalSchematic.SchematicId,
                    originalSchematicName = originalSchematic.SchematicName,
                    originalCreator = originalSchematic.CreatorName,
                    violationDescription = description,
                    violatorName = violatorName,
                    evidenceDescription = evidenceDescription,
                    reportDate = DateTime.Now
                };
                
                // Сохраняем отчет локально
                SaveReportLocally(report);
                
                // Отправляем отчет на вашу почту
                SendEmailToAdmin(report);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reporting violation: {ex.Message}");
                return false;
            }
        }
        
        // Сохраняет отчет локально
        private static void SaveReportLocally(ViolationReport report)
        {
            // Создаем директорию для отчетов, если её нет
            string reportsDirectory = Path.Combine(Application.persistentDataPath, "ViolationReports");
            if (!Directory.Exists(reportsDirectory))
            {
                Directory.CreateDirectory(reportsDirectory);
            }
            
            // Формируем имя файла
            string fileName = $"Violation_{report.originalSchematicId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(reportsDirectory, fileName);
            
            // Сериализуем отчет в JSON
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
            
            // Записываем в файл
            File.WriteAllText(filePath, json);
            
            Debug.Log($"Violation report saved to: {filePath}");
        }
        
        // Отправляет отчет на вашу почту
        private static void SendEmailToAdmin(ViolationReport report)
        {
            // Запускаем отправку в отдельном потоке, чтобы не блокировать UI
            Task.Run(() => {
                try
                {
                    // Формируем тему письма
                    string subject = $"[SCP SL Schematic Protection] Violation Report: {report.originalSchematicName}";
                    
                    // Формируем тело письма
                    StringBuilder body = new StringBuilder();
                    body.AppendLine("<h2>SCP SL Schematic Protection - Violation Report</h2>");
                    body.AppendLine("<hr>");
                    body.AppendLine("<h3>Original Schematic Information</h3>");
                    body.AppendLine($"<p><strong>Name:</strong> {report.originalSchematicName}</p>");
                    body.AppendLine($"<p><strong>ID:</strong> {report.originalSchematicId}</p>");
                    body.AppendLine($"<p><strong>Creator:</strong> {report.originalCreator}</p>");
                    body.AppendLine("<hr>");
                    body.AppendLine("<h3>Reporter Information</h3>");
                    body.AppendLine($"<p><strong>Name:</strong> {report.reporterName}</p>");
                    body.AppendLine($"<p><strong>Email:</strong> {report.reporterEmail}</p>");
                    body.AppendLine("<hr>");
                    body.AppendLine("<h3>Violation Details</h3>");
                    body.AppendLine($"<p><strong>Violator:</strong> {report.violatorName}</p>");
                    body.AppendLine("<p><strong>Description:</strong></p>");
                    body.AppendLine($"<p>{report.violationDescription}</p>");
                    body.AppendLine("<p><strong>Evidence:</strong></p>");
                    body.AppendLine($"<p>{report.evidenceDescription}</p>");
                    body.AppendLine("<hr>");
                    body.AppendLine($"<p><em>Report generated on {report.reportDate}</em></p>");
                    
                    // Создаем объект сообщения
                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress(string.IsNullOrEmpty(report.reporterEmail) ? 
                        "noreply@scpslschematicprotection.com" : report.reporterEmail);
                    mail.To.Add(AdminEmail);
                    mail.Subject = subject;
                    mail.Body = body.ToString();
                    mail.IsBodyHtml = true;
                    
                    // Настраиваем SMTP-клиент
                    // Примечание: для реального использования вам потребуется настроить
                    // собственный SMTP-сервер или использовать сервис отправки email
                    using (SmtpClient smtp = new SmtpClient())
                    {
                        // Пример настройки для Gmail
                        smtp.Host = "smtp.gmail.com";
                        smtp.Port = 587;
                        smtp.EnableSsl = true;
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        
                        // Настройки аутентификации
                        // ВАЖНО: В реальном приложении никогда не храните учетные данные в коде
                        // Это лишь пример, который нужно заменить на безопасное решение
                        smtp.Credentials = new NetworkCredential(
                            EditorPrefs.GetString("SchematicProtection_SmtpUsername", ""), 
                            EditorPrefs.GetString("SchematicProtection_SmtpPassword", "")
                        );
                        
                        // Отправляем сообщение
                        smtp.Send(mail);
                        
                        Debug.Log($"Violation report email sent to {AdminEmail}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to send email: {ex.Message}");
                    
                    // Для отладки записываем полную информацию об ошибке в лог
                    string logPath = Path.Combine(Application.persistentDataPath, "email_errors.log");
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Error: {ex.ToString()}\n\n");
                }
            });
        }
        
        // Показывает окно отчета о нарушении
        public static void ShowReportViolationWindow(SchematicComponent originalSchematic)
        {
            ReportViolationWindow.ShowWindow(originalSchematic);
        }
        
        // Показывает окно настройки SMTP
        public static void ShowSmtpSettingsWindow()
        {
            SmtpSettingsWindow.ShowWindow();
        }
    }
    
    // Окно для отправки отчета о нарушении
    public class ReportViolationWindow : EditorWindow
    {
        private SchematicComponent originalSchematic;
        private string violatorName = "";
        private string violationDescription = "";
        private string evidenceDescription = "";
        private string reporterEmail = "";
        
        public static void ShowWindow(SchematicComponent schematic)
        {
            ReportViolationWindow window = GetWindow<ReportViolationWindow>(true, "Report Violation");
            window.originalSchematic = schematic;
            window.minSize = new Vector2(400, 400);
            
            // Загружаем email из настроек
            window.reporterEmail = EditorPrefs.GetString("SchematicProtection_ReportingEmail", "");
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField("Report Copyright Violation", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (originalSchematic == null)
            {
                EditorGUILayout.HelpBox("No schematic selected. Please close this window and try again.", MessageType.Error);
                return;
            }
            
            EditorGUILayout.LabelField($"Original Schematic: {originalSchematic.SchematicName}");
            EditorGUILayout.LabelField($"Creator: {originalSchematic.CreatorName}");
            EditorGUILayout.LabelField($"ID: {originalSchematic.SchematicId}");
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Your Contact Information", EditorStyles.boldLabel);
            
            // Email для обратной связи
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Your Email:", GUILayout.Width(120));
            reporterEmail = EditorGUILayout.TextField(reporterEmail);
            EditorGUILayout.EndHorizontal();
            
            if (reporterEmail != EditorPrefs.GetString("SchematicProtection_ReportingEmail", ""))
            {
                EditorPrefs.SetString("SchematicProtection_ReportingEmail", reporterEmail);
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Violator Information", EditorStyles.boldLabel);
            violatorName = EditorGUILayout.TextField("Violator Name:", violatorName);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Violation Description", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Describe how your schematic was used without permission.", MessageType.Info);
            violationDescription = EditorGUILayout.TextArea(violationDescription, GUILayout.Height(60));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Evidence Description", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Describe any evidence you have (screenshots, links, etc.)", MessageType.Info);
            evidenceDescription = EditorGUILayout.TextArea(evidenceDescription, GUILayout.Height(60));
            
            EditorGUILayout.Space();
            
            // Проверяем, настроены ли SMTP-настройки
            bool isSmtpConfigured = !string.IsNullOrEmpty(EditorPrefs.GetString("SchematicProtection_SmtpUsername", ""));
            
            if (!isSmtpConfigured)
            {
                EditorGUILayout.HelpBox("SMTP settings are not configured. Email notifications will not be sent. Click 'Configure SMTP' to set up email sending.", MessageType.Warning);
                
                if (GUILayout.Button("Configure SMTP", GUILayout.Height(25)))
                {
                    ReportingSystem.ShowSmtpSettingsWindow();
                }
                
                EditorGUILayout.Space();
            }
            
            if (GUILayout.Button("Submit Report", GUILayout.Height(30)))
            {
                if (string.IsNullOrEmpty(violatorName) || string.IsNullOrEmpty(violationDescription))
                {
                    EditorUtility.DisplayDialog("Incomplete Information", 
                        "Please fill in the violator name and violation description.", "OK");
                    return;
                }
                
                bool success = ReportingSystem.ReportViolation(
                    originalSchematic, 
                    violatorName, 
                    violationDescription, 
                    evidenceDescription
                );
                
                if (success)
                {
                    string message = isSmtpConfigured ? 
                        "Your violation report has been submitted successfully and an email notification has been sent." :
                        "Your violation report has been saved locally, but email notification was not sent due to missing SMTP configuration.";
                    
                    EditorUtility.DisplayDialog("Report Submitted", message, "OK");
                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog("Submission Failed", 
                        "Failed to submit the violation report. Please try again later.", "OK");
                }
            }
        }
    }
    
    // Окно настройки SMTP
    public class SmtpSettingsWindow : EditorWindow
    {
        private string smtpHost = "smtp.gmail.com";
        private int smtpPort = 587;
        private string smtpUsername = "";
        private string smtpPassword = "";
        private bool enableSsl = true;
        
        public static void ShowWindow()
        {
            SmtpSettingsWindow window = GetWindow<SmtpSettingsWindow>(true, "SMTP Settings");
            window.minSize = new Vector2(400, 250);
            
            // Загружаем настройки
            window.smtpHost = EditorPrefs.GetString("SchematicProtection_SmtpHost", "smtp.gmail.com");
            window.smtpPort = EditorPrefs.GetInt("SchematicProtection_SmtpPort", 587);
            window.smtpUsername = EditorPrefs.GetString("SchematicProtection_SmtpUsername", "");
            window.smtpPassword = EditorPrefs.GetString("SchematicProtection_SmtpPassword", "");
            window.enableSsl = EditorPrefs.GetBool("SchematicProtection_SmtpEnableSsl", true);
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField("SMTP Server Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These settings are required to send email notifications about violations. For Gmail, you may need to create an App Password.", MessageType.Info);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // SMTP Host
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SMTP Host:", GUILayout.Width(120));
            smtpHost = EditorGUILayout.TextField(smtpHost);
            EditorGUILayout.EndHorizontal();
            
            // SMTP Port
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SMTP Port:", GUILayout.Width(120));
            smtpPort = EditorGUILayout.IntField(smtpPort);
            EditorGUILayout.EndHorizontal();
            
            // Enable SSL
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Enable SSL:", GUILayout.Width(120));
            enableSsl = EditorGUILayout.Toggle(enableSsl);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // SMTP Username
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Username:", GUILayout.Width(120));
            smtpUsername = EditorGUILayout.TextField(smtpUsername);
            EditorGUILayout.EndHorizontal();
            
            // SMTP Password
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Password:", GUILayout.Width(120));
            smtpPassword = EditorGUILayout.PasswordField(smtpPassword);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save Settings", GUILayout.Height(30)))
            {
                SaveSettings();
                EditorUtility.DisplayDialog("Settings Saved", "SMTP settings have been saved successfully.", "OK");
                Close();
            }
            
            if (GUILayout.Button("Test Connection", GUILayout.Height(30)))
            {
                TestSmtpConnection();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("Note: Password is stored in EditorPrefs and is not encrypted. For production use, consider implementing a more secure storage method.", MessageType.Warning);
        }
        
        private void SaveSettings()
        {
            EditorPrefs.SetString("SchematicProtection_SmtpHost", smtpHost);
            EditorPrefs.SetInt("SchematicProtection_SmtpPort", smtpPort);
            EditorPrefs.SetString("SchematicProtection_SmtpUsername", smtpUsername);
            EditorPrefs.SetString("SchematicProtection_SmtpPassword", smtpPassword);
            EditorPrefs.SetBool("SchematicProtection_SmtpEnableSsl", enableSsl);
        }
        
        private void TestSmtpConnection()
        {
            // Сохраняем текущие настройки перед тестированием
            SaveSettings();
            
            // Запускаем тест в отдельном потоке
            Task.Run(() => {
                try
                {
                    using (SmtpClient client = new SmtpClient(smtpHost, smtpPort))
                    {
                        client.EnableSsl = enableSsl;
                        client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                        client.Timeout = 10000; // 10 секунд таймаут
                        
                        // Создаем тестовое сообщение
                        MailMessage testMail = new MailMessage();
                        testMail.From = new MailAddress(smtpUsername);
                        testMail.To.Add("tlpanari@gmail.com");
                        testMail.Subject = "SCP SL Schematic Protection - SMTP Test";
                        testMail.Body = "This is a test email from SCP SL Schematic Protection plugin.";
                        
                        // Пытаемся отправить сообщение
                        client.Send(testMail);
                        
                        // Если дошли до этой точки, значит отправка успешна
                        EditorApplication.delayCall += () => {
                            EditorUtility.DisplayDialog("Test Successful", 
                                "SMTP connection test was successful! A test email has been sent to tlpanari@gmail.com.", "OK");
                        };
                    }
                }
                catch (Exception ex)
                {
                    // Обрабатываем ошибку
                    string errorMessage = ex.Message;
                    
                    EditorApplication.delayCall += () => {
                        EditorUtility.DisplayDialog("Test Failed", 
                            $"SMTP connection test failed: {errorMessage}\n\nPlease check your settings and try again.", "OK");
                    };
                }
            });
        }
    }
}