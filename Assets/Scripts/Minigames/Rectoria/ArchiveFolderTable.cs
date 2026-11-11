using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusQuest.Rectoria
{
    // Append values to preserve the existing serialized color IDs.
    public enum ArchiveFolderColor { Red, Green, Yellow, Purple, Blue, Orange }

    [Serializable]
    public sealed class ArchiveFolder
    {
        public ArchiveFolderColor color;
        public int year;
        public bool IsAnswer => color == ArchiveFolderColor.Blue && year == 2008;
        public string Label => ColorName + " · " + year;
        public string ColorName
        {
            get
            {
                switch (color)
                {
                    case ArchiveFolderColor.Red: return "Roja";
                    case ArchiveFolderColor.Green: return "Verde";
                    case ArchiveFolderColor.Yellow: return "Amarilla";
                    case ArchiveFolderColor.Purple: return "Morada";
                    case ArchiveFolderColor.Orange: return "Naranja";
                    default: return "Azul";
                }
            }
        }
        public Color Tint
        {
            get
            {
                switch (color)
                {
                    case ArchiveFolderColor.Red: return new Color32(204, 87, 90, 255);
                    case ArchiveFolderColor.Green: return new Color32(98, 159, 110, 255);
                    case ArchiveFolderColor.Yellow: return new Color32(225, 190, 88, 255);
                    case ArchiveFolderColor.Purple: return new Color32(157, 111, 186, 255);
                    case ArchiveFolderColor.Orange: return new Color32(218, 142, 73, 255);
                    default: return new Color32(89, 145, 211, 255);
                }
            }
        }
    }

    [CreateAssetMenu(menuName = "Campus Quest/Archive Folder Table")]
    public sealed class ArchiveFolderTable : ScriptableObject
    {
        [Tooltip("The nine exact color/year pairs from GUION RECTORIA. Do not invent missing entries.")]
        public ArchiveFolder[] folders = Array.Empty<ArchiveFolder>();

        public bool IsValid
        {
            get
            {
                if (folders == null || folders.Length != 9) return false;
                var unique = new HashSet<string>();
                int answers = 0;
                foreach (var folder in folders)
                {
                    if (folder == null || folder.year <= 0 || !Enum.IsDefined(typeof(ArchiveFolderColor), folder.color)
                        || !unique.Add(folder.Label)) return false;
                    if (folder.IsAnswer) answers++;
                }
                return answers == 1;
            }
        }
    }
}
