using System;
using System.IO;

namespace AFSLib
{
    public abstract class Entry
    {
        /// <summary>
        /// Raw name of the entry. It can contain special characters like "/", "\" or ":", and it can be the same name as other entries. Don't use this name to extract a file into the operating system.
        /// </summary>
        public string RawName => rawName;
        protected string rawName;

        /// <summary>
        /// The name of the entry. It will be unique and won't contain special characters like "/", "\" or ":". It can't be longer than 32 characters, including extension.
        /// </summary>
        public string Name => name;
        private string name;

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

            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            for (int c = 0; c < invalidCharacters.Length; c++)
            {
                if (name.Contains(invalidCharacters[c].ToString()))
                {
                    throw new ArgumentException($"The entry name \"{name}\" can't contain the character: \"{invalidCharacters[c]}\"", nameof(name));
                }
            }

            rawName = name;
            afs.UpdateEntriesNames();
        }

        internal void UpdateName(string name)
        {
            this.name = name;
        }

        internal abstract Stream GetStream();
    }
}