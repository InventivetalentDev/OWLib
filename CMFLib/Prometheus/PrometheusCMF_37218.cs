﻿using static CMFLib.CMFHandler;

namespace CMFLib.Prometheus {
    [CMFMetadata(AutoDetectVersion = true, BuildVersions = new uint[] { }, App = CMFApplication.Prometheus)]
    public class PrometheusCMF_37218 : ICMFProvider {
        public byte[] Key(CMFHeader header, string name, byte[] digest, int length) {
            byte[] buffer = new byte[length];

            uint kidx = Constrain(header.BuildVersion * length);
            uint increment = kidx % 61;
            for (int i = 0; i != length; ++i) {
                buffer[i] = Keytable[kidx % 512];
                kidx += increment;
            }

            return buffer;
        }

        public byte[] IV(CMFHeader header, string name, byte[] digest, int length) {
            byte[] buffer = new byte[length];

            uint kidx = (uint) (digest[7] + (ushort) (header.DataCount & 511));
            for (int i = 0; i != length; ++i) {
                buffer[i] = Keytable[kidx % 512];
                switch (kidx % 3) {
                    case 0:
                        kidx += 103;
                        break;
                    case 1:
                        kidx = 4 * kidx % header.BuildVersion;
                        break;
                    case 2:
                        --kidx;
                        break;
                }

                buffer[i] ^= digest[(kidx + header.BuildVersion) % SHA1_DIGESTSIZE];
            }

            return buffer;
        }
        
        private static readonly byte[] Keytable = {
            0x9D, 0xCD, 0x12, 0x1D, 0x66, 0x98, 0xEE, 0x2F, 0x98, 0xE9, 0x44, 0x9A, 0x3C, 0x6B, 0x87, 0x4C, 
            0xFD, 0x95, 0x23, 0x1A, 0x3D, 0xA5, 0xA0, 0xFF, 0x95, 0x65, 0x17, 0x0C, 0x13, 0xAB, 0x4C, 0xA0, 
            0x63, 0x80, 0x20, 0x7D, 0xD4, 0x6B, 0xC4, 0x4D, 0xA6, 0x30, 0x31, 0x86, 0xEF, 0x58, 0x48, 0x9F, 
            0x83, 0x81, 0x24, 0x83, 0x01, 0x9E, 0x54, 0x30, 0x52, 0x9C, 0x3A, 0x1C, 0x21, 0x89, 0x46, 0x98, 
            0xFF, 0x78, 0x17, 0x55, 0xF9, 0xC2, 0x7B, 0x58, 0x72, 0xB1, 0x7B, 0x0C, 0x8E, 0xFB, 0x7B, 0x7E,
            0xF7, 0x1F, 0x98, 0x4C, 0x19, 0xBD, 0xAF, 0xB0, 0xEF, 0x40, 0xC6, 0x60, 0x95, 0x2D, 0x9B, 0x1A, 
            0x03, 0x5B, 0x35, 0xC8, 0xC4, 0xA0, 0xEC, 0x1E, 0xAE, 0xBD, 0x6C, 0x32, 0x9B, 0x83, 0x38, 0xBB, 
            0x6C, 0x60, 0x73, 0x0C, 0xAC, 0x4C, 0x8F, 0x75, 0x18, 0x50, 0xCA, 0x29, 0x04, 0x75, 0x8C, 0x58,
            0x2A, 0xB8, 0x8A, 0x30, 0x52, 0x6A, 0xD5, 0xBD, 0x2E, 0xA7, 0xCD, 0x7C, 0x43, 0x05, 0x73, 0xC2, 
            0x1C, 0x8B, 0xC5, 0xBA, 0xD4, 0xCF, 0xF1, 0x53, 0xB0, 0xE4, 0xC6, 0x3B, 0x2A, 0x4B, 0x1F, 0x49, 
            0x28, 0x33, 0xF3, 0xC0, 0x22, 0xE9, 0xF9, 0x4A, 0x9F, 0xC4, 0x7A, 0x95, 0xB9, 0x1F, 0x07, 0xFA, 
            0x54, 0x63, 0x83, 0x1F, 0x48, 0xF3, 0x15, 0xE9, 0xD1, 0xD4, 0x91, 0x46, 0x32, 0xE7, 0x27, 0x44, 
            0x72, 0xCA, 0x4C, 0x71, 0x03, 0x7F, 0x36, 0xDA, 0x1C, 0xCB, 0xBD, 0x5D, 0xF2, 0x4A, 0x24, 0x3B, 
            0xB6, 0x73, 0x1C, 0x0F, 0x2A, 0x63, 0x88, 0xC9, 0xDF, 0x30, 0x9A, 0x16, 0x94, 0x49, 0x87, 0x1E, 
            0xA4, 0x46, 0xD3, 0xB2, 0xF3, 0xB3, 0xE3, 0x96, 0x01, 0x81, 0x22, 0xD6, 0x54, 0xFD, 0xBB, 0x4D, 
            0x35, 0x05, 0xCE, 0x2F, 0x50, 0x3A, 0x77, 0xA6, 0x06, 0xA9, 0x32, 0xBA, 0x7B, 0xAE, 0x1C, 0xD1,
            0xF9, 0xF7, 0xC9, 0xF2, 0xE9, 0x83, 0xDE, 0xC3, 0x3C, 0xFF, 0xDE, 0xDB, 0x01, 0xC4, 0x43, 0x0C, 
            0x54, 0x85, 0xD5, 0xC5, 0xD4, 0x0F, 0x8B, 0x96, 0x46, 0xE7, 0x1C, 0x0C, 0x2D, 0xDF, 0xAD, 0xC6, 
            0xEE, 0xA5, 0x3B, 0x06, 0xD7, 0x09, 0xBC, 0x9F, 0x81, 0xA6, 0xA6, 0x75, 0x09, 0xEF, 0xD0, 0xFD,
            0xC6, 0xFB, 0xD1, 0xCE, 0xCF, 0xD9, 0x07, 0xD5, 0x0F, 0x77, 0x7B, 0x8D, 0xF5, 0x91, 0xF0, 0x73, 
            0x4E, 0x59, 0x3F, 0x65, 0xE8, 0xAC, 0x3B, 0x68, 0xE5, 0xD1, 0x06, 0x4F, 0xD2, 0x60, 0x91, 0x5F, 
            0x95, 0x89, 0xD8, 0x1B, 0x1C, 0x28, 0x7D, 0x58, 0xE3, 0x4A, 0xD7, 0x39, 0x98, 0xB0, 0x27, 0x79, 
            0xF1, 0xF0, 0x98, 0x33, 0x47, 0x49, 0x1D, 0xD7, 0xC9, 0xC5, 0x4A, 0x03, 0xC4, 0x2C, 0x25, 0x81, 
            0x65, 0x31, 0x74, 0x75, 0x9D, 0x5C, 0x3F, 0xDF, 0x70, 0xD4, 0x88, 0xDD, 0xEB, 0x7F, 0x46, 0x3B, 
            0xDB, 0x10, 0xDF, 0x9A, 0x6B, 0xD7, 0x3E, 0x76, 0x86, 0x45, 0xF4, 0x06, 0x29, 0x3E, 0x30, 0x16, 
            0x8A, 0x38, 0x61, 0x36, 0x63, 0x2A, 0x4B, 0x98, 0x90, 0x93, 0x1C, 0x34, 0x25, 0x20, 0xF7, 0x5D, 
            0x2F, 0xE3, 0xB9, 0x97, 0x15, 0x21, 0x8A, 0x14, 0x51, 0x68, 0xC4, 0xAA, 0x66, 0xBF, 0xD3, 0x7B,
            0xD8, 0x5F, 0x5A, 0x15, 0xA2, 0xFA, 0x43, 0xDA, 0x9B, 0x6C, 0xF0, 0x56, 0xE3, 0x42, 0xC6, 0x27, 
            0x4A, 0x15, 0x22, 0xEF, 0xEE, 0x90, 0xA1, 0xEE, 0x4E, 0x9D, 0x60, 0xB9, 0xAF, 0x12, 0x44, 0x8E, 
            0x4F, 0x77, 0x14, 0xA2, 0xAF, 0x9C, 0x11, 0x88, 0x94, 0xF2, 0xA1, 0x18, 0x03, 0x49, 0x8E, 0xC6,
            0x25, 0xBF, 0x25, 0x77, 0xCA, 0x12, 0x43, 0x36, 0x8D, 0x3A, 0x47, 0xAD, 0xD3, 0x77, 0x94, 0xAC, 
            0xEE, 0x5A, 0x1D, 0x9D, 0x50, 0x8A, 0xF2, 0x92, 0x69, 0xBD, 0x96, 0x19, 0x6C, 0x28, 0xFC, 0x04
        };
    }
}