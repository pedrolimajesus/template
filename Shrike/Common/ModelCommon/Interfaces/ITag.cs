namespace Lok.Unik.ModelCommon.Interfaces
{
    using System;

    public interface ITag
    {
        Uri Path { get; }

        string Attribute { get; }

        string Value { get; }
    }
}