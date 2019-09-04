using System;
using UnityEngine;
using VRM.QuickMetaLoader.Model;

namespace VRM.QuickMetaLoader
{
    public unsafe struct BlazingFastMetaLoader
    {
        private byte* bytes;
        private long length;
        private byte* binaryStart;
        private long binaryRestLength;
        private long binaryChunkSize;

        public BlazingFastMetaLoader(byte* bytes, long length)
        {
            if (bytes == null) throw new ArgumentNullException();
            if (length <= 0) throw new ArgumentOutOfRangeException(length + " : length shouldn't be minus!");
            this = default;
            this.bytes = bytes;
            this.length = length;
        }

        private bool Initialize()
        {
            var findIndexOfVrm = ValidateMagicWords(ref bytes, ref length) && ValidateVersion(ref bytes, ref length) && ReadTotalLength(ref bytes, ref length, out _) && ReadChunkLength(ref bytes, ref length, out var chunkLength) && ValidateChunkTypeWhetherToBeJson(ref bytes, ref length) && ReadBinaryData(bytes + chunkLength, length - chunkLength, out binaryStart, out binaryRestLength, out binaryChunkSize) && FindIndexOf___VRM___(ref bytes, ref length);
            if (findIndexOfVrm)
            {
                length -= 7L;
                bytes += 7;
            }
            return findIndexOfVrm;
        }

        public bool ReadMeta(VRMMetaObject metaObject)
        {
            if (!Initialize()) throw new ArgumentException();
            var bytes0 = bytes;
            var length0 = length;
            if (!ReadExporterVersion(ref bytes0, ref length0, out var strSBytes0, out var strLength0))
                return false;
            var bytes1 = bytes;
            var length1 = length;
            if (!ReadMeta(ref bytes1, ref length1, out var strSBytes1, out var strLength1))
                return false;
            metaObject.ExporterVersion = new string(strSBytes0, 0, strLength0);
            var metaString = new string(strSBytes1, 0, strLength1);
            var QMeta = JsonUtility.FromJson<QuickMetaObject>(metaString);
            QMeta.PushMeta(ref metaObject);
            return true;
        }

        private static bool ReadMeta(ref byte* bytes, ref long length, out sbyte* strSBytes, out int strLength)
        {
            strSBytes = default;
            strLength = default;
            if (!FindIndexOf___meta(ref bytes, ref length))
            {
                return false;
            }
            bytes += 7;
            length -= 7L;
            strSBytes = (sbyte*)bytes;
            var tmpLength = length;
            if (!FindIndexOf(ref bytes, ref length, 0x7d)) // }
            {
                return false;
            }
            strLength = (int)(tmpLength - length) + 1;
            bytes++;
            length--;
            return true;
        }

        private static bool FindIndexOf___meta(ref byte* bytes, ref long length)
        {
            if (!FindIndexOf(ref bytes, ref length, 0x74656d22U, 0x2261, 0x3a)) // "meta":
            {
                Debug.LogWarning(@"'""meta"":' not found");
                return false;
            }
            return true;
        }

        private static bool FindIndexOf___VRM___(ref byte* bytes, ref long length)
        {
            if (!FindIndexOf(ref bytes, ref length, 0x4d525622, 0x3a22, 0x7b)) // "VRM ": {
            {
                Debug.LogWarning(@"'""VRM"":{' not found");
                return false;
            }
            return true;
        }

        private static bool FindIndexOf___exporterVersion(ref byte* bytes, ref long length)
        {
            if (!FindIndexOf(ref bytes, ref length, 0x6574726f70786522UL, 0x6e6f697372655672UL, 0x3a22, 0x22)) // "exporterVersion":"
            {
                Debug.LogWarning(@"'""exporterVersion"":""' not found");
                return false;
            }
            return true;
        }

        private static bool ReadExporterVersion(ref byte* bytes, ref long length, out sbyte* strSBytes, out int strLength)
        {
            strSBytes = default;
            strLength = default;
            if (!FindIndexOf___exporterVersion(ref bytes, ref length))
            {
                return false;
            }
            bytes += 19;
            length -= 19L;
            strSBytes = (sbyte*)bytes;
            var tmpLength = length;
            if (!FindIndexOf(ref bytes, ref length, 0x22))
            {
                return false;
            }
            strLength = (int)(tmpLength - length);
            bytes++;
            length--;
            return true;
        }

        private static bool FindIndexOf(ref byte* bytes, ref long length, ulong first8, ulong second8, ushort third2, byte last)
        {
            const long count = 19L;
            for (; length >= count; bytes++, length--)
            {
                if (*(ulong*)bytes == first8 && *(ulong*)(bytes + 8) == second8 && *(ushort*)(bytes + 16) == third2 && bytes[count - 1L] == last)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool FindIndexOf(ref byte* bytes, ref long length, uint first4, ushort second2, byte last)
        {
            const long count = 7L;
            for (; length >= count; bytes++, length--)
            {
                if (*(uint*)bytes == first4 && *(ushort*)(bytes + 4) == second2 && bytes[count - 1L] == last)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool FindIndexOf(ref byte* bytes, ref long length, uint value)
        {
            for (; length >= 4L; bytes++, length--)
            {
                if (*(uint*)bytes == value)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool FindIndexOf(ref byte* bytes, ref long length, byte value)
        {
            for (; length >= 1L; bytes++, length--)
            {
                if (*bytes == value)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ReadBinaryData(byte* bytes, long length, out byte* binaryStart, out long binaryLength, out long chunkSize)
        {
            binaryStart = bytes;
            binaryLength = length;
            if (binaryLength < 4L)
            {
                chunkSize = default;
                return false;
            }
            chunkSize = *(int*)binaryStart;
            binaryStart += 4;
            binaryLength -= 4L;

            if (binaryLength < 4L) return false;
            var chunkTypeBytes = *(uint*)binaryStart;
            if (chunkTypeBytes != 0x4e4942)
            {
                Debug.LogWarning("unknown chunk type: " + new string((sbyte*)binaryStart, 0, 3));
                return false;
            }
            binaryStart += 4;
            binaryLength -= 4L;
            return true;
        }

        private static bool ValidateChunkTypeWhetherToBeJson(ref byte* bytes, ref long length)
        {
            if (length < 4)
            {
                Debug.LogWarning("Contents should contain chunk type name");
                return false;
            }
            if (*(uint*)bytes != 0x4e4f534a)
            {
                Debug.LogWarning("unknown chunk type:" + new string((sbyte*)bytes, 0, 4));
                return false;
            }
            bytes += 4;
            length -= 4L;
            return true;
        }

        private static bool ReadChunkLength(ref byte* bytes, ref long length, out uint chunkLength)
        {
            if (length < 4)
            {
                Debug.LogWarning("Contents should contain chunk length");
                chunkLength = default;
                return false;
            }
            chunkLength = *(uint*)bytes;
            bytes += 4;
            length -= 4L;
            return true;
        }

        private static bool ReadTotalLength(ref byte* bytes, ref long length, out uint totalLength)
        {
            if (length < 4)
            {
                Debug.LogWarning("Contents should contain total length");
                totalLength = default;
                return false;
            }
            totalLength = *(uint*)bytes;
            bytes += 4;
            length -= 4L;
            return true;
        }

        private static bool ValidateVersion(ref byte* bytes, ref long length)
        {
            if (length < 4)
            {
                Debug.LogWarning("Contents should contain version number");
                return false;
            }
            if (*(uint*)bytes != UniGLTF.glbImporter.GLB_VERSION)
            {
                Debug.LogWarning("Unknown Version: " + *(uint*)bytes);
            }
            bytes += 4;
            length -= 4L;
            return true;
        }

        private static bool ValidateMagicWords(ref byte* bytes, ref long length)
        {
            if (length < 4)
            {
                Debug.LogWarning("Contents should contain MAGIC BYTES!");
                return false;
            }
            if (*(uint*)bytes != 0x46546c67)
            {
                Debug.LogWarning("MAGIC BYTES is different from original bytes.");
                return false;
            }
            bytes += 4;
            length -= 4L;
            return true;
        }
    }
}