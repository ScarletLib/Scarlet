using Scarlet.Utilities;
using System;

namespace Scarlet.Components
{
    public interface IGps
    {
        Tuple<float, float> GetCoords();
    }
}
