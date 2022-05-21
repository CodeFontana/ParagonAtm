namespace ParagonAtmLibrary.Models;

public class ScreenCoordinates
{
    public ScreenCoordinates()
    {

    }
    
    public ScreenCoordinates(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public float x { get; set; }
    public float y { get; set; }
}



