using System;
using System.Collections.Generic;
using System.Windows.Forms;

// Vegas API
using ScriptPortal.Vegas;

public class EntryPoint
{
    public void FromVegas(Vegas vegas)
    {
        // Get the last clip end time
        Timecode lastTime = GetEndTime(vegas);

        double seconds = TimecodeToSeconds(lastTime,vegas);

        MessageBox.Show("Last Clip End Time: " + lastTime.ToString(), "Vegas Script");
        MessageBox.Show("Total Seconds: " + seconds.ToString(), "Second Check");

        // Define times where you want to create regions (in seconds)
        double[] regionTimes = { 5.0, 10.0, 15.0, 20.5 }; // Example times

        // Define default region duration in seconds (30 min)
        double regionDuration = 30 * 60;

        foreach (double timeInSeconds in regionTimes)
        {
            // Convert seconds to Vegas Timecode
            Timecode startTime = Timecode.FromSeconds(timeInSeconds);
            Timecode endTime = startTime + Timecode.FromSeconds(regionDuration);

            // Create a new region (explicitly use ScriptPortal.Vegas.Region)
            ScriptPortal.Vegas.Region newRegion = new ScriptPortal.Vegas.Region(startTime, endTime, "Region at " + timeInSeconds + "s");

            // Add the region to the project
            vegas.Project.Regions.Add(newRegion);
        }

        Console.WriteLine("Regions created successfully.");
    }

    // FIXED: No LINQ, fully compatible with Vegas scripting
    public Timecode GetEndTime(Vegas vegas)
    {
        Timecode lastEndTime = new Timecode(0); 
        
        
        foreach (Track track in vegas.Project.Tracks)
        {
            if (track.IsVideo())
            {
                foreach (TrackEvent trackEvent in track.Events)
                {

                    Timecode endTime = trackEvent.Start + trackEvent.Length;
                    
                    if (endTime > lastEndTime)
                    {
                        lastEndTime = endTime;
                    }
                }
            }
        }
        if (lastEndTime == null)
            throw new Exception("No End Time Detected");

        return lastEndTime;
    }

    public double TimecodeToSeconds(Timecode timecode, Vegas vegas)
    {
        double frameRate = vegas.Project.Video.FrameRate;
        return timecode.FrameCount / frameRate;
    }
}
