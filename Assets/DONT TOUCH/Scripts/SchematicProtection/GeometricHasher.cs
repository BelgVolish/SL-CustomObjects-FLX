using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DONT_TOUCH.Scripts.SchematicProtection
{
    public static class GeometricHasher
    {
        // Структура для хранения информации о примитиве
        private struct PrimitiveInfo
        {
            public string Type;
            public Vector3 Position;
            public Vector3 Rotation;
            public Vector3 Scale;
            public int MaterialHash;
            public int ChildIndex;
            public int ParentIndex;
            
            public override string ToString()
            {
                return $"{Type}|{FormatVector(Position)}|{FormatVector(Rotation)}|{FormatVector(Scale)}|{MaterialHash}|{ChildIndex}|{ParentIndex}";
            }
            
            private string FormatVector(Vector3 v)
            {
                // Округляем до 3 знаков после запятой для устойчивости хеша
                return $"{Math.Round(v.x, 3)},{Math.Round(v.y, 3)},{Math.Round(v.z, 3)}";
            }
        }
        
        // Обновляет геометрический хеш для схематика
        public static void UpdateGeometricHash(SchematicComponent schematic)
        {
            string hash = CalculateGeometricHash(schematic.gameObject);
            schematic.SetGeometricHash(hash);
            
            // Сохраняем изменения
            EditorUtility.SetDirty(schematic);
        }
        
        // Вычисляет геометрический хеш для объекта и его дочерних объектов
        public static string CalculateGeometricHash(GameObject root)
        {
            List<PrimitiveInfo> primitives = new List<PrimitiveInfo>();
            
            // Собираем информацию о всех примитивах
            CollectPrimitives(root, primitives);
            
            // Сортируем примитивы для обеспечения детерминированного порядка
            primitives = primitives.OrderBy(p => p.Type)
                                   .ThenBy(p => p.Position.x)
                                   .ThenBy(p => p.Position.y)
                                   .ThenBy(p => p.Position.z)
                                   .ToList();
            
            // Создаем строковое представление всех примитивов
            StringBuilder sb = new StringBuilder();
            foreach (var primitive in primitives)
            {
                sb.AppendLine(primitive.ToString());
            }
            
            // Вычисляем хеш
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                byte[] hashBytes = sha.ComputeHash(bytes);
                
                StringBuilder hashBuilder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    hashBuilder.Append(hashBytes[i].ToString("x2"));
                }
                
                return hashBuilder.ToString();
            }
        }
        
        // Рекурсивно собирает информацию о примитивах
        private static void CollectPrimitives(GameObject obj, List<PrimitiveInfo> primitives, int parentIndex = -1)
        {
            // Определяем тип примитива
            string type = "Unknown";
            
            // Проверяем стандартные примитивы Unity
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                if (meshFilter.sharedMesh.name.Contains("Cube"))
                    type = "Cube";
                else if (meshFilter.sharedMesh.name.Contains("Sphere"))
                    type = "Sphere";
                else if (meshFilter.sharedMesh.name.Contains("Cylinder"))
                    type = "Cylinder";
                else if (meshFilter.sharedMesh.name.Contains("Plane"))
                    type = "Plane";
                else if (meshFilter.sharedMesh.name.Contains("Capsule"))
                    type = "Capsule";
                else
                    type = "CustomMesh:" + meshFilter.sharedMesh.name;
            }
            
            // Вычисляем хеш материала, если есть
            int materialHash = 0;
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                materialHash = renderer.sharedMaterial.name.GetHashCode();
            }
            
            // Текущий индекс примитива
            int currentIndex = primitives.Count;
            
            // Добавляем информацию о примитиве
            primitives.Add(new PrimitiveInfo
            {
                Type = type,
                Position = obj.transform.localPosition,
                Rotation = obj.transform.localEulerAngles,
                Scale = obj.transform.localScale,
                MaterialHash = materialHash,
                ChildIndex = obj.transform.childCount,
                ParentIndex = parentIndex
            });
            
            // Рекурсивно обрабатываем дочерние объекты
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                CollectPrimitives(obj.transform.GetChild(i).gameObject, primitives, currentIndex);
            }
        }
        
        // Проверяет геометрический хеш схематика
        public static bool VerifyGeometricHash(SchematicComponent schematic)
        {
            string storedHash = schematic.GetGeometricHash();
            string currentHash = CalculateGeometricHash(schematic.gameObject);
            
            return storedHash == currentHash;
        }
        
        // Сравнивает геометрический хеш двух схематиков
        public static float CompareGeometricHashes(GameObject obj1, GameObject obj2)
        {
            string hash1 = CalculateGeometricHash(obj1);
            string hash2 = CalculateGeometricHash(obj2);
            
            if (hash1 == hash2)
                return 1.0f; // Полное совпадение
                
            // Для частичного сравнения нужно реализовать более сложную логику
            // Например, сравнение отдельных примитивов или использование алгоритма сходства строк
            
            return 0.0f; // По умолчанию: нет совпадения
        }
    }
}