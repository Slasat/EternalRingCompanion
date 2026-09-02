using System;
using System.IO;
using System.Text;

namespace EternalRingCompanion.Core;

/// <summary>
/// Resolves a named PE export's RVA by parsing the export directory directly out of the file
/// on disk. LoadLibraryEx(LOAD_LIBRARY_AS_DATAFILE) + GetProcAddress only maps the file's raw
/// bytes and fails for an export whose storage lives in a zero-initialized (BSS-like) region
/// not physically present in the file — which is exactly PCSX2's "EEmem" global pointer. We
/// only need the RVA number; the live pointer value is read separately from the running process.
/// (Copied from PS2Trainer.)
/// </summary>
internal static class ExportResolver
{
    public static long? ResolveRva(string filePath, string exportName)
    {
        byte[] data = File.ReadAllBytes(filePath);

        int peOffset = BitConverter.ToInt32(data, 0x3C);
        int coffOffset = peOffset + 4;
        ushort numberOfSections = BitConverter.ToUInt16(data, coffOffset + 2);
        ushort sizeOfOptionalHeader = BitConverter.ToUInt16(data, coffOffset + 16);
        int optHeaderOffset = coffOffset + 20;
        ushort magic = BitConverter.ToUInt16(data, optHeaderOffset);
        bool isPE32Plus = magic == 0x20b;

        int dataDirOffset = optHeaderOffset + (isPE32Plus ? 112 : 96);
        uint exportRva = BitConverter.ToUInt32(data, dataDirOffset);
        if (exportRva == 0) return null;

        int sectionHeaderOffset = optHeaderOffset + sizeOfOptionalHeader;
        var sections = new System.Collections.Generic.List<(uint VirtualAddress, uint VirtualSize, uint PointerToRawData)>();
        for (int i = 0; i < numberOfSections; i++)
        {
            int off = sectionHeaderOffset + i * 40;
            uint virtualSize = BitConverter.ToUInt32(data, off + 8);
            uint virtualAddress = BitConverter.ToUInt32(data, off + 12);
            uint pointerToRawData = BitConverter.ToUInt32(data, off + 20);
            sections.Add((virtualAddress, virtualSize, pointerToRawData));
        }

        int RvaToOffset(uint rva)
        {
            foreach (var s in sections)
            {
                if (rva >= s.VirtualAddress && rva < s.VirtualAddress + Math.Max(s.VirtualSize, 1))
                    return (int)(s.PointerToRawData + (rva - s.VirtualAddress));
            }
            throw new InvalidOperationException($"RVA 0x{rva:X} not in any section");
        }

        int expOff = RvaToOffset(exportRva);
        uint numberOfNames = BitConverter.ToUInt32(data, expOff + 24);
        uint addressOfFunctions = BitConverter.ToUInt32(data, expOff + 28);
        uint addressOfNames = BitConverter.ToUInt32(data, expOff + 32);
        uint addressOfNameOrdinals = BitConverter.ToUInt32(data, expOff + 36);

        int namesOff = RvaToOffset(addressOfNames);
        int ordinalsOff = RvaToOffset(addressOfNameOrdinals);
        int functionsOff = RvaToOffset(addressOfFunctions);

        for (int i = 0; i < numberOfNames; i++)
        {
            uint nameRva = BitConverter.ToUInt32(data, namesOff + i * 4);
            int nameOff = RvaToOffset(nameRva);
            int end = nameOff;
            while (data[end] != 0) end++;
            string name = Encoding.ASCII.GetString(data, nameOff, end - nameOff);

            if (name == exportName)
            {
                ushort ordinalIndex = BitConverter.ToUInt16(data, ordinalsOff + i * 2);
                uint funcRva = BitConverter.ToUInt32(data, functionsOff + ordinalIndex * 4);
                return funcRva;
            }
        }
        return null;
    }
}
