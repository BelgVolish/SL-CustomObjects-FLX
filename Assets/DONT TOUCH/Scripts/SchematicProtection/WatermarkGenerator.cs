using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DONT_TOUCH.Scripts.SchematicProtection
{
    public static class WatermarkGenerator
    {
        // Максимальное смещение для водяного знака (очень маленькое, незаметное)
        private const float MAX_OFFSET = 0.001f;
        
        // Применяет водяной знак к схематику
        public static void ApplyWatermark(SchematicComponent schematic)
        {
            // Генерируем уникальный ключ на основе ID схематика и имени создателя
            string seed = schematic.SchematicId + schematic.CreatorName;
            
            // Создаем генератор случайных чисел с детерминированным зерном
            System.Random random = new System.Random(seed.GetHashCode());
            
            // Список примененных смещений для сохранения в метаданных
            List<string> watermarkData = new List<string>();
            
            // Добавляем невидимые маркеры (скрытые пустые объекты)
            GameObject watermarkRoot = new GameObject("_wm_" + schematic.SchematicId.Substring(0, 8));
            watermarkRoot.transform.SetParent(schematic.transform);
            
            // Скрываем объект в иерархии и делаем неактивным
            watermarkRoot.hideFlags = HideFlags.HideInHierarchy;
            watermarkRoot.SetActive(false);
            
            // Создаем уникальную структуру водяного знака
            int markerCount = random.Next(3, 7); // Случайное число маркеров
            for (int i = 0; i < markerCount; i++)
            {
                GameObject marker = new GameObject($"_wm_marker_{i}");
                marker.transform.SetParent(watermarkRoot.transform);
                
                // Устанавливаем случайное положение, но с определенным паттерном
                float x = (float)random.NextDouble() * 2 - 1;
                float y = (float)random.NextDouble() * 2 - 1;
                float z = (float)random.NextDouble() * 2 - 1;
                
                marker.transform.localPosition = new Vector3(x, y, z) * 0.1f;
                
                // Записываем информацию о маркере
                watermarkData.Add($"m{i}:{x},{y},{z}");
            }
            
            // Применяем небольшие смещения к существующим примитивам
            ApplyOffsetsToPrimitives(schematic.gameObject, random, watermarkData);
            
            // Сохраняем данные водяного знака в схематике
            schematic.SetWatermarkData(watermarkData.ToArray());
            
            // Сохраняем изменения
            EditorUtility.SetDirty(schematic);
            EditorUtility.SetDirty(watermarkRoot);
        }
        
        // Применяет небольшие смещения к примитивам
        private static void ApplyOffsetsToPrimitives(GameObject root, System.Random random, List<string> watermarkData)
        {
            // Получаем все примитивы
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();
            
            // Выбираем некоторые примитивы для применения смещений
            int offsetCount = Mathf.Min(10, meshFilters.Length);
            
            for (int i = 0; i < offsetCount; i++)
            {
                // Выбираем случайный примитив
                int index = random.Next(meshFilters.Length);
                GameObject primitive = meshFilters[index].gameObject;
                
                // Применяем очень маленькое смещение
                float offsetX = ((float)random.NextDouble() * 2 - 1) * MAX_OFFSET;
                float offsetY = ((float)random.NextDouble() * 2 - 1) * MAX_OFFSET;
                float offsetZ = ((float)random.NextDouble() * 2 - 1) * MAX_OFFSET;
                
                // Сохраняем исходное положение
                Vector3 originalPosition = primitive.transform.localPosition;
                
                // Применяем смещение
                primitive.transform.localPosition = new Vector3(
                    originalPosition.x + offsetX,
                    originalPosition.y + offsetY,
                    originalPosition.z + offsetZ
                );
                
                // Записываем информацию о смещении
                string primitivePath = GetGameObjectPath(primitive);
                watermarkData.Add($"o:{primitivePath}:{offsetX},{offsetY},{offsetZ}");
                
                // Помечаем объект как измененный
                EditorUtility.SetDirty(primitive);
            }
        }
        
        // Получает путь к объекту в иерархии
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        // Проверяет наличие водяного знака в схематике
        public static bool VerifyWatermark(SchematicComponent schematic)
        {
            string[] watermarkData = schematic.GetWatermarkData();
            if (watermarkData == null || watermarkData.Length == 0)
                return false;
                
            // Проверяем наличие скрытого объекта водяного знака
            Transform watermarkRoot = schematic.transform.Find("_wm_" + schematic.SchematicId.Substring(0, 8));
            if (watermarkRoot == null)
                return false;
                
            // Здесь можно добавить дополнительные проверки маркеров и смещений
            
            return true;
        }
        
        // Сравнивает водяные знаки двух схематиков
        public static float CompareWatermarks(SchematicComponent schematic1, SchematicComponent schematic2)
        {
            string[] watermarkData1 = schematic1.GetWatermarkData();
            string[] watermarkData2 = schematic2.GetWatermarkData();
            
            if (watermarkData1 == null || watermarkData2 == null || 
                watermarkData1.Length == 0 || watermarkData2.Length == 0)
                return 0.0f;
                
            // Считаем количество совпадающих элементов
            int matchCount = 0;
            foreach (string data1 in watermarkData1)
            {
                foreach (string data2 in watermarkData2)
                {
                    if (data1 == data2)
                    {
                        matchCount++;
                        break;
                    }
                }
            }
            
            // Вычисляем процент совпадения
            int totalElements = Math.Max(watermarkData1.Length, watermarkData2.Length);
            return (float)matchCount / totalElements;
        }
    }
}