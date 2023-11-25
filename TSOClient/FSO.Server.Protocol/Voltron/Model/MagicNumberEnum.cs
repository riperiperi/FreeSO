namespace FSO.Server.Protocol.Voltron.Model
{
    public enum MagicNumberEnum
    {
        VAL_E4E9B25D,
        VAL_A46E47DC,
        VAL_B5D630DD,
        VAL_211D969E,
        VAL_D042E9D6,
        VAL_3998426C,
        VAL_75FFC299,
        VAL_87102FBA,
        VAL_486B3F7E,
        VAL_14656565,
        VAL_0C0CB1E6,
        VAL_227BED60,
        VAL_77A46FE8,
        VAL_9062B390,
        VAL_13639511,
        VAL_94016F9F,
        VAL_B2C6ED04,
        VAL_67281FA4,
        VAL_404D4CB1,
        VAL_0350C667,
        VAL_C5521020,
        VAL_8D7F9BF2,
        VAL_55D2AE37,
        VAL_74AF57D1,
        VAL_D3B216CA,
        VAL_4BD42E0C,
        VAL_DFAEA619,
        VAL_E3FC98F9,
        VAL_1C364C5D,
        VAL_196546F1,
        VAL_9702D009,
        VAL_DC07F91F,
        VAL_AF3D3F6D
    }

    public static class MagicNumberEnumUtils
    {
        public static int ToInt(this MagicNumberEnum value)
        {
            switch (value)
            {
                case MagicNumberEnum.VAL_E4E9B25D:
                    return 0;
                case MagicNumberEnum.VAL_A46E47DC:
                    return 0;
                case MagicNumberEnum.VAL_B5D630DD:
                    return 4;
                case MagicNumberEnum.VAL_211D969E:
                    return 0;
                case MagicNumberEnum.VAL_D042E9D6:
                    return 0;
                case MagicNumberEnum.VAL_3998426C:
                    return 0;
                case MagicNumberEnum.VAL_75FFC299:
                    return 0;
                case MagicNumberEnum.VAL_87102FBA:
                    return 0;
                case MagicNumberEnum.VAL_486B3F7E:
                    return 2;
                case MagicNumberEnum.VAL_14656565:
                    return 20;
                case MagicNumberEnum.VAL_0C0CB1E6:
                    return 0;
                case MagicNumberEnum.VAL_227BED60:
                    return 2;
                case MagicNumberEnum.VAL_77A46FE8:
                    return 0;
                case MagicNumberEnum.VAL_9062B390:
                    return 100;
                case MagicNumberEnum.VAL_13639511:
                    return 0;
                case MagicNumberEnum.VAL_94016F9F:
                    return 10;
                case MagicNumberEnum.VAL_B2C6ED04:
                    return 0;
                case MagicNumberEnum.VAL_67281FA4:
                    return 20;
                case MagicNumberEnum.VAL_404D4CB1:
                    return 0;
                case MagicNumberEnum.VAL_0350C667:
                    return 30;
                case MagicNumberEnum.VAL_C5521020:
                    return 10;
                case MagicNumberEnum.VAL_8D7F9BF2:
                    return 3;
                case MagicNumberEnum.VAL_55D2AE37:
                    return 10;
                case MagicNumberEnum.VAL_74AF57D1:
                    return 100;
                case MagicNumberEnum.VAL_D3B216CA:
                    return 64;
                case MagicNumberEnum.VAL_4BD42E0C:
                    return 64;
                case MagicNumberEnum.VAL_DFAEA619:
                    return 30;
                case MagicNumberEnum.VAL_E3FC98F9:
                    return 60;
                case MagicNumberEnum.VAL_1C364C5D:
                    return 30;
                case MagicNumberEnum.VAL_196546F1:
                    return 2;
                case MagicNumberEnum.VAL_9702D009:
                    return 60;
                case MagicNumberEnum.VAL_DC07F91F:
                    return 0;
                case MagicNumberEnum.VAL_AF3D3F6D:
                    return 60;
            }
            return 0;
        }

        public static float ToFloat(this MagicNumberEnum value)
        {
            switch (value)
            {
                case MagicNumberEnum.VAL_E4E9B25D:
                    return 0.2f;
                case MagicNumberEnum.VAL_A46E47DC:
                    return 0.2f;
                case MagicNumberEnum.VAL_B5D630DD:
                    return 4.0f;
                case MagicNumberEnum.VAL_211D969E:
                    return 0.1f;
                case MagicNumberEnum.VAL_D042E9D6:
                    return 0.5f;
                case MagicNumberEnum.VAL_3998426C:
                    return 0.5f;
                case MagicNumberEnum.VAL_75FFC299:
                    return 0.5f;
                case MagicNumberEnum.VAL_87102FBA:
                    return 0.5f;
                case MagicNumberEnum.VAL_486B3F7E:
                    return 1.0f;
                case MagicNumberEnum.VAL_14656565:
                    return 10.0f;
                case MagicNumberEnum.VAL_0C0CB1E6:
                    return 0.5f;
                case MagicNumberEnum.VAL_227BED60:
                    return 4.0f;
                case MagicNumberEnum.VAL_77A46FE8:
                    return 0.5f;
                case MagicNumberEnum.VAL_9062B390:
                    return 100.0f;
                case MagicNumberEnum.VAL_13639511:
                    return 0.5f;
                case MagicNumberEnum.VAL_94016F9F:
                    return 2.0f;
                case MagicNumberEnum.VAL_B2C6ED04:
                    return 0.5f;
                case MagicNumberEnum.VAL_67281FA4:
                    return 10.0f;
                case MagicNumberEnum.VAL_404D4CB1:
                    return 0.5f;
                case MagicNumberEnum.VAL_0350C667:
                    return 2.0f;
                case MagicNumberEnum.VAL_C5521020:
                    return 2.0f;
                case MagicNumberEnum.VAL_8D7F9BF2:
                    return 3.0f;
                case MagicNumberEnum.VAL_55D2AE37:
                    return 1.0f;
                case MagicNumberEnum.VAL_74AF57D1:
                    return 2.0f;
                case MagicNumberEnum.VAL_D3B216CA:
                    return 64.0f;
                case MagicNumberEnum.VAL_4BD42E0C:
                    return 64.0f;
                case MagicNumberEnum.VAL_DFAEA619:
                    return 30.0f;
                case MagicNumberEnum.VAL_E3FC98F9:
                    return 60.0f;
                case MagicNumberEnum.VAL_1C364C5D:
                    return 6.0f;
                case MagicNumberEnum.VAL_196546F1:
                    return 0.5f;
                case MagicNumberEnum.VAL_9702D009:
                    return 60.0f;
                case MagicNumberEnum.VAL_DC07F91F:
                    return 0.5f;
                case MagicNumberEnum.VAL_AF3D3F6D:
                    return 60.0f;
            }
            return 0.0f;
        }

        public static uint ToID(this MagicNumberEnum value)
        {
            switch (value)
            {
                case MagicNumberEnum.VAL_E4E9B25D:
                    return 0xE4E9B25D;
                case MagicNumberEnum.VAL_A46E47DC:
                    return 0xA46E47DC;
                case MagicNumberEnum.VAL_B5D630DD:
                    return 0xB5D630DD;
                case MagicNumberEnum.VAL_211D969E:
                    return 0x211D969E;
                case MagicNumberEnum.VAL_D042E9D6:
                    return 0xD042E9D6;
                case MagicNumberEnum.VAL_3998426C:
                    return 0x3998426C;
                case MagicNumberEnum.VAL_75FFC299:
                    return 0x75FFC299;
                case MagicNumberEnum.VAL_87102FBA:
                    return 0x87102FBA;
                case MagicNumberEnum.VAL_486B3F7E:
                    return 0x486B3F7E;
                case MagicNumberEnum.VAL_14656565:
                    return 0x14656565;
                case MagicNumberEnum.VAL_0C0CB1E6:
                    return 0x0C0CB1E6;
                case MagicNumberEnum.VAL_227BED60:
                    return 0x227BED60;
                case MagicNumberEnum.VAL_77A46FE8:
                    return 0x77A46FE8;
                case MagicNumberEnum.VAL_9062B390:
                    return 0x9062B390;
                case MagicNumberEnum.VAL_13639511:
                    return 0x13639511;
                case MagicNumberEnum.VAL_94016F9F:
                    return 0x94016F9F;
                case MagicNumberEnum.VAL_B2C6ED04:
                    return 0xB2C6ED04;
                case MagicNumberEnum.VAL_67281FA4:
                    return 0x67281FA4;
                case MagicNumberEnum.VAL_404D4CB1:
                    return 0x404D4CB1;
                case MagicNumberEnum.VAL_0350C667:
                    return 0x0350C667;
                case MagicNumberEnum.VAL_C5521020:
                    return 0xC5521020;
                case MagicNumberEnum.VAL_8D7F9BF2:
                    return 0x8D7F9BF2;
                case MagicNumberEnum.VAL_55D2AE37:
                    return 0x55D2AE37;
                case MagicNumberEnum.VAL_74AF57D1:
                    return 0x74AF57D1;
                case MagicNumberEnum.VAL_D3B216CA:
                    return 0xD3B216CA;
                case MagicNumberEnum.VAL_4BD42E0C:
                    return 0x4BD42E0C;
                case MagicNumberEnum.VAL_DFAEA619:
                    return 0xDFAEA619;
                case MagicNumberEnum.VAL_E3FC98F9:
                    return 0xE3FC98F9;
                case MagicNumberEnum.VAL_1C364C5D:
                    return 0x1C364C5D;
                case MagicNumberEnum.VAL_196546F1:
                    return 0x196546F1;
                case MagicNumberEnum.VAL_9702D009:
                    return 0x9702D009;
                case MagicNumberEnum.VAL_DC07F91F:
                    return 0xDC07F91F;
                case MagicNumberEnum.VAL_AF3D3F6D:
                    return 0xAF3D3F6D;
            }
            return 0;
        }

        public static MagicNumberEnum FromID(uint id)
        {
            switch (id)
            {
                case 0xE4E9B25D:
                    return MagicNumberEnum.VAL_E4E9B25D;
                case 0xA46E47DC:
                    return MagicNumberEnum.VAL_A46E47DC;
                case 0xB5D630DD:
                    return MagicNumberEnum.VAL_B5D630DD;
                case 0x211D969E:
                    return MagicNumberEnum.VAL_211D969E;
                case 0xD042E9D6:
                    return MagicNumberEnum.VAL_D042E9D6;
                case 0x3998426C:
                    return MagicNumberEnum.VAL_3998426C;
                case 0x75FFC299:
                    return MagicNumberEnum.VAL_75FFC299;
                case 0x87102FBA:
                    return MagicNumberEnum.VAL_87102FBA;
                case 0x486B3F7E:
                    return MagicNumberEnum.VAL_486B3F7E;
                case 0x14656565:
                    return MagicNumberEnum.VAL_14656565;
                case 0x0C0CB1E6:
                    return MagicNumberEnum.VAL_0C0CB1E6;
                case 0x227BED60:
                    return MagicNumberEnum.VAL_227BED60;
                case 0x77A46FE8:
                    return MagicNumberEnum.VAL_77A46FE8;
                case 0x9062B390:
                    return MagicNumberEnum.VAL_9062B390;
                case 0x13639511:
                    return MagicNumberEnum.VAL_13639511;
                case 0x94016F9F:
                    return MagicNumberEnum.VAL_94016F9F;
                case 0xB2C6ED04:
                    return MagicNumberEnum.VAL_B2C6ED04;
                case 0x67281FA4:
                    return MagicNumberEnum.VAL_67281FA4;
                case 0x404D4CB1:
                    return MagicNumberEnum.VAL_404D4CB1;
                case 0x0350C667:
                    return MagicNumberEnum.VAL_0350C667;
                case 0xC5521020:
                    return MagicNumberEnum.VAL_C5521020;
                case 0x8D7F9BF2:
                    return MagicNumberEnum.VAL_8D7F9BF2;
                case 0x55D2AE37:
                    return MagicNumberEnum.VAL_55D2AE37;
                case 0x74AF57D1:
                    return MagicNumberEnum.VAL_74AF57D1;
                case 0xD3B216CA:
                    return MagicNumberEnum.VAL_D3B216CA;
                case 0x4BD42E0C:
                    return MagicNumberEnum.VAL_4BD42E0C;
                case 0xDFAEA619:
                    return MagicNumberEnum.VAL_DFAEA619;
                case 0xE3FC98F9:
                    return MagicNumberEnum.VAL_E3FC98F9;
                case 0x1C364C5D:
                    return MagicNumberEnum.VAL_1C364C5D;
                case 0x196546F1:
                    return MagicNumberEnum.VAL_196546F1;
                case 0x9702D009:
                    return MagicNumberEnum.VAL_9702D009;
                case 0xDC07F91F:
                    return MagicNumberEnum.VAL_DC07F91F;
                case 0xAF3D3F6D:
                    return MagicNumberEnum.VAL_AF3D3F6D;
            }
            return default(MagicNumberEnum);
        }
    }
}
