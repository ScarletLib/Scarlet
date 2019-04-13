using System;
namespace Scarlet.Components.Sensors
{
    public interface IGps
    {
        Tuple<float, float> GetCoords();
    }

}
