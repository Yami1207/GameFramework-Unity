#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OptimalPerformance
{
    public static class CheckMaterial
    {
        #region Unused Property

        private delegate void EachUnusedPropertiesFunc(SerializedProperty parent, int _index, SerializedProperty target);
        private delegate void EachUnusedPropertiesFinishFunc(SerializedObject so);

        /// <summary>
        /// 获得所有未使用的属性名
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static string[] GetUnusedPropertyNames(Material material)
        {
            System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
            ForeachUnusedProperties(material, (parent, index, target) =>
            {
                list.Add(target.stringValue);
            }, null);
            return list.ToArray();
        }

        /// <summary>
        /// 删除所有未使用的属性
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool RemoveUnusedProperties(Material material)
        {
            bool hasModifiedProperties = false;
            ForeachUnusedProperties(material,
               (parent, index, target) =>
               {
                   parent.DeleteArrayElementAtIndex(index);
               },
               (so) =>
               {
                   hasModifiedProperties = so.ApplyModifiedProperties();
               }
               );
            return hasModifiedProperties;
        }

        /// <summary>
        /// 遍历每个unused属性
        /// </summary>
        /// <param name="material"></param>
        /// <param name="func"></param>
        /// <param name="finishFunc"></param>
        private static void ForeachUnusedProperties(Material material, EachUnusedPropertiesFunc func, EachUnusedPropertiesFinishFunc finishFunc)
        {
            if (material == null)
                return;

            var serializedObject = new SerializedObject(material);
            serializedObject.Update();

            var savedProp = serializedObject.FindProperty("m_SavedProperties");

            // Tex Envs
            var texProp = savedProp.FindPropertyRelative("m_TexEnvs");
            for (int i = texProp.arraySize - 1; i >= 0; --i)
            {
                var property = texProp.GetArrayElementAtIndex(i).FindPropertyRelative("first");
                var propertyName = property.stringValue;
                if (!material.HasProperty(propertyName))
                {
                    func.Invoke(texProp, i, property);
                }
            }

            // Floats
            var floatProp = savedProp.FindPropertyRelative("m_Floats");
            for (int i = floatProp.arraySize - 1; i >= 0; --i)
            {
                var property = floatProp.GetArrayElementAtIndex(i).FindPropertyRelative("first");
                var propertyName = property.stringValue;
                if (!material.HasProperty(propertyName))
                {
                    func.Invoke(floatProp, i, property);
                }
            }

            // Colors
            var _colorProp = savedProp.FindPropertyRelative("m_Colors");
            for (int i = _colorProp.arraySize - 1; i >= 0; --i)
            {
                var property = _colorProp.GetArrayElementAtIndex(i).FindPropertyRelative("first");
                var propertyName = property.stringValue;
                if (!material.HasProperty(propertyName))
                {
                    func.Invoke(_colorProp, i, property);
                }
            }

            if (finishFunc != null)
                finishFunc.Invoke(serializedObject);
        }

        #endregion
    }
}

#endif
