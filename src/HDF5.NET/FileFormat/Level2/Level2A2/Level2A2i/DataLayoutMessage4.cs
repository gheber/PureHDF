﻿namespace HDF5.NET
{
    public class DataLayoutMessage4 : DataLayoutMessage3
    {
        #region Constructors

        internal DataLayoutMessage4(H5BinaryReader reader, Superblock superblock, byte version)
            : base(reader, superblock, version)
        {
            //
        }

        #endregion
    }
}
