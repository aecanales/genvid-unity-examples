using UnityEngine;
using System;
using TMPro;

// This rotates the -hand game objects to create the in game clock.
public class Clock : MonoBehaviour 
{
    public TextMeshPro Text;
    
    public GameObject HourHand;
    public GameObject MinuteHand;
    public GameObject SecondHand;
    
    void Update()
    {
        DateTime now = DateTime.Now;

        Text.text = now.ToString("hh:mm:ss tt");

        // We get the hour in a 12-hour format.
        float hour = float.Parse(now.ToString("%h"));

        // We convert all values to floats to operate on them later on.
        float minute = (float) now.Minute;
        float second = (float) now.Second;
        float millisecond = (float) now.Millisecond;

        // To have a smoother clock  we add the immediate smaller unit to each value and use that to calculate the hand angle.
        float smoothHour = hour + minute / 60;
        float smoothMinute = minute + second / 60;
        float smoothSecond = second + millisecond / 1000;

        // We get the angle for each hand and set them.
        float hourAngle = getAngle(1, 12, smoothHour);
        float minuteAngle = getAngle(0, 60, smoothMinute);
        float secondAngle = getAngle(0, 60, smoothSecond);

        HourHand.transform.rotation = Quaternion.Euler(0, 0, hourAngle);
        MinuteHand.transform.rotation = Quaternion.Euler(0, 0, minuteAngle);
        SecondHand.transform.rotation = Quaternion.Euler(0, 0, secondAngle);
    }

    // Uses linear interpolation between two values to get the correct angle for a hand based on the time.
    float getAngle(float min, float max, float value)
    {
        float lerp = Mathf.InverseLerp(min, max, value);
        return Mathf.Lerp(360, 0, lerp);
    }
}