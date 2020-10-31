using System;
using System.IO;

namespace AFSLib
{
    public abstract class Entry
    {
        /// <summary>
        /// Name of the entry. It can't be longer than 32 characters, including extension.
        /// </summary>
        public string Name => name;
        protected string name;

        /// <summary>
        /// Size of the entry data.
        /// </summary>
        public uint Size => size;
        protected uint size;

        /// <summary>
        /// Date of the last time the entry was modified.
        /// </summary>
        public DateTime LastWriteTime => lastWriteTime;
        protected DateTime lastWriteTime;

        /// <summary>
        /// Sometimes it's the entry size, sometimes not?
        /// </summary>
        public uint Unknown => unknown;
        protected uint unknown;

        /// <summary>
        /// An AFS file can contain multiple entries with the same name. Trying to extract those entries, each one would overwrite the previous one. So this provides a unique name that won't cause conflicts.
        /// </summary>
        public string UniqueName => uniqueName;
        private string uniqueName;

        private readonly AFS afs;

        internal Entry(AFS afs)
        {
            this.afs = afs;
        }

        public void Rename(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length > AFS.MAX_ENTRY_NAME_LENGTH)
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"Entry name can't be longer than {AFS.MAX_ENTRY_NAME_LENGTH} characters: \"{name}\".");
            }

            this.name = name;
            afs.UpdateDuplicatedEntries();
        }

        internal void UpdateUniqueName(uint duplicateCount)
        {
            if (duplicateCount > 0)
                uniqueName = $"{Path.GetFileNameWithoutExtension(Name)} ({duplicateCount}){Path.GetExtension(Name)}";
            else
                uniqueName = Name;
        }

        internal abstract Stream GetStream();
    }
}