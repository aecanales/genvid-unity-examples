using UnityEngine;
using System;

public class Clock : MonoBehaviour 
{
    public GameObject HourHand;
    public GameObject MinuteHand;
    
    void Update()
    {
        DateTime now = DateTime.Now;

        // We get the hour in a 12-hour format.
        float hour = float.Parse(now.ToString("%h"));
        float minute = (float) now.Minute;
        float second = (float) now.Second;

        float smoothHour = hour + minute / 60;
        float smoothMinute = minute + second / 60;

        float hourAngle = getAngle(1, 12, smoothHour);
        float minuteAngle = getAngle(0, 60, smoothMinute);

        HourHand.transform.rotation = Quaternion.Euler(0, 0, hourAngle);
        MinuteHand.transform.rotation = Quaternion.Euler(0, 0, minuteAngle);
    }

    float getAngle(float min, float max, float value)
    {
        float lerp = Mathf.InverseLerp(min, max, value);

        return Mathf.Lerp(360, 0, lerp);
    }
}