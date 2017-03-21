using UnityEngine;
using System.Collections;

public class News
{
    public string Text { get; set; }
    public float DelayMilliseconds { get; set; }
    public float DurationMilliseconds { get; set; }
    public MiscIcon Icon;
}


/// <summary>
/// Information for things like "[E] Pick Up"
/// </summary>
public class PromptInfo
{
    public string Key { get; set; }
    public string Description { get; set; }

    public string SecondaryKey { get; set; }
    public string SecondaryDescription { get; set; }
    public bool HasSecondary
    {
        get
        {
            return SecondaryKey != null;
        }
    }

    /// <summary>
    /// 0 to 1f percentage
    /// </summary>
    public float Progress { get; set; }
    /// <summary>
    /// Whether to show the progressbar, default to false
    /// </summary>
    public bool UsesProgress { get; set; }

    public string ItalicizedText { get; set; }
}

public struct LinkablePrompts
{
    public PromptInfo HoverWhenNoneSelected;
    public PromptInfo HoverWhenOneSelected;
    public News WhenCompleted;
}

public static class NewsSource
{
    public static News DroppodHere = new News()
    {
        Text = "Incoming Drop Pod",
        DurationMilliseconds = 10000,
        Icon = MiscIcon.Rocket
    };
    public static News AlgaeHarvestable = new News()
    {
        Text = "Algae Harvest Ready",
        DurationMilliseconds = 10000,
        Icon = MiscIcon.Harvest
    };
    public static News ToolOpenHint = new News()
    {
        Text = "[TAB] for tools",
        DurationMilliseconds = 10000,
        Icon = MiscIcon.Information
    };
    public static News FOneHint = new News()
    {
        Text = "[F1] for help",
        DelayMilliseconds = 10000,
        DurationMilliseconds = 10000,
        Icon = MiscIcon.Information
    };
}

public static class Prompts {
    //hints and prompts for actions
    public static PromptInfo StartBulkheadBridgeHint = new PromptInfo()
    {
        Description = "Select bulkhead to connect",
        Key = "E"
    };
    public static PromptInfo EndBulkheadBridgeHint = new PromptInfo()
    {
        Description = "Connect bulkheads",
        Key = "E"
    };

    public static News BulkheadBridgeCompleted = new News()
    {
        Text = "Bulkheads connected",
        DurationMilliseconds = 1500
    };
    public static PromptInfo StartGasPipeHint = new PromptInfo()
    {
        Description = "Run new pipeline",
        Key = "E"
    };
    public static PromptInfo EndGasPipeHint = new PromptInfo()
    {
        Description = "End pipeline here",
        Key = "E"
    };
    public static News GasPipeCompleted = new News()
    {
        Text = "Gas Pipe connected",
        DurationMilliseconds = 1500,
        Icon = MiscIcon.Pipe
    };
    public static PromptInfo StartPowerPlugHint = new PromptInfo()
    {
        Description = "Run new powerline",
        Key = "E"
    };
    public static PromptInfo EndPowerPlugHint = new PromptInfo()
    {
        Description = "Connect powerline here",
        Key = "E"
    };
    public static News PowerPlugCompleted = new News()
    {
        Text = "Power connected",
        DurationMilliseconds = 1500,
        Icon = MiscIcon.Plug
    };
    public static PromptInfo DriveRoverPrompt = new PromptInfo()
    {
        Description = "Drive Rover",
        Key = "E"
    };
    public static PromptInfo RoverDoorPrompt = new PromptInfo()
    {
        Description = "Move Hatch",
        Key = "E"
    };
    public static PromptInfo PickupHint = new PromptInfo()
    {
        Description = "Pick up",
        Key = "LMB"
    };
    internal static PromptInfo DropHint = new PromptInfo()
    {
        Description = "Drop",
        Key = "LMB"
    };
    internal static PromptInfo ConstructHint = new PromptInfo()
    {
        Description = "Construct",
        Key = "E",
        UsesProgress = true
    };
    internal static PromptInfo OpenDoorHint = new PromptInfo()
    {
        Description = "Open Door",
        Key = "E"
    };
    internal static PromptInfo CloseDoorHint = new PromptInfo()
    {
        Description = "Close Door",
        Key = "E"
    };
    internal static PromptInfo DoorLockedHint = new PromptInfo()
    {
        Description = "Door Locked",
        Key = null
    };
    internal static PromptInfo GenericButtonHint = new PromptInfo()
    {
        Description = "Press Button",
        Key = "E"
    };
    internal static PromptInfo DrinkWaterHint = new PromptInfo()
    {
        Description = "Drink Water",
        Key = "E"
    };
    internal static PromptInfo FoodPrepPowderHint = new PromptInfo()
    {
        Description = "Prepare Shake",
        Key = "E"
    };
    internal static PromptInfo FoodPrepBiomassHint = new PromptInfo()
    {
        Description = "Prepare Meal",
        Key = "E"
    };
    internal static PromptInfo PlanConstructionZoneHint = new PromptInfo()
    {
        Description = "Begin Construction Here",
        Key = "E"
    };
    internal static PromptInfo InvalidPipeHint = new PromptInfo()
    {
        Description = "Invalid connection",
        Key = null
    };
    internal static PromptInfo ExcavateHint = new PromptInfo()
    {
        Description = "Excavate",
        Key = "E",
        UsesProgress = true
    };
    internal static LinkablePrompts BulkheadBridgePrompts = new LinkablePrompts()
    {
        HoverWhenNoneSelected = StartBulkheadBridgeHint,
        HoverWhenOneSelected = EndBulkheadBridgeHint,
        WhenCompleted = BulkheadBridgeCompleted
    };
    internal static LinkablePrompts GasPipePrompts = new LinkablePrompts()
    {
        HoverWhenNoneSelected = StartGasPipeHint,
        HoverWhenOneSelected = EndGasPipeHint,
        WhenCompleted = GasPipeCompleted
    };
    internal static LinkablePrompts PowerPlugPrompts = new LinkablePrompts()
    {
        HoverWhenNoneSelected = StartPowerPlugHint,
        HoverWhenOneSelected = EndPowerPlugHint,
        WhenCompleted = PowerPlugCompleted
    };

    internal static PromptInfo LadderOnHint = new PromptInfo()
    {
        Description = "Mount Ladder",
        Key = "E"
    };

    internal static PromptInfo LadderOffHint = new PromptInfo()
    {
        Description = "Dismount Ladder",
        Key = "E"
    };

    internal static PromptInfo MealOrganicEatHint = new PromptInfo()
    {
        Description = "Eat Meal",
        Key = "E"
    };

    internal static PromptInfo MealPreparedEatHint = MealOrganicEatHint;

    internal static PromptInfo MealShakeEatHint = new PromptInfo()
    {
        Description = "Drink Meal",
        Key = "E"
    };
    internal static PromptInfo ExistingPipeRemovalHint = new PromptInfo()
    {
        Description = "Remove Connection",
        Key = "E"
    };
    internal static PromptInfo PayloadDisassembleHint = new PromptInfo()
    {
        Description = "Disassemble Lander",
        Key = "E"
    };
    internal static PromptInfo PlaceStuffHint = new PromptInfo()
    {
        Description = "Place Here",
        Key = "E"
    };
    internal static PromptInfo PlaceFloorplanHint = new PromptInfo()
    {
        Description = "Place Here",
        Key = "E"
    };
    internal static PromptInfo PostItFinishHint = new PromptInfo()
    {
        Description = "Stop Writing Note",
        Key = "Esc"
    };
    internal static PromptInfo PostItDeleteHint = new PromptInfo()
    {
        Description = "Scrap Note",
        Key = "E"
    };
    internal static PromptInfo BedEnterHint = new PromptInfo()
    {
        Description = "Sleep",
        Key = "E"
    };
    internal static PromptInfo BedExitHint = new PromptInfo()
    {
        Description = "Wake Up",
        Key = "E",
        SecondaryDescription = "Sleep Until Morning",
        SecondaryKey = "Z"
    };
    internal static PromptInfo SleepTilMorningExitHint = new PromptInfo()
    {
        Description = "Wake Up",
        Key = "E",
    };
    internal static PromptInfo EVAChargeHint = new PromptInfo()
    {
        Description = "Charge",
        Key = "E",
        UsesProgress = true
    };
    internal static PromptInfo ReportHint = new PromptInfo()
    {
        Description = "Industrial Report",
        Key = "E"
    };
    internal static PromptInfo PowerSwitchOffHint = new PromptInfo()
    {
        Description = "Turn off power",
        Key = "E"
    };
    internal static PromptInfo PowerSwitchOnHint = new PromptInfo()
    {
        Description = "Turn on power",
        Key = "E"
    };

    internal static PromptInfo StopPumpingOutHint = new PromptInfo()
    {
        Description = "Disable Pump",
        Key = "LMB"
    };
    internal static PromptInfo StopPumpingInHint = new PromptInfo()
    {
        Description = "Disable Pump",
        Key = "RMB"
    };
    internal static PromptInfo TurnPumpOnHint = new PromptInfo()
    {
        Description = "Pump Into Tank",
        Key = "LMB",
        SecondaryDescription = "Pump Out of Tank",
        SecondaryKey = "RMB"
    };

    public static PromptInfo DrillHint = new PromptInfo()
    {
        Description = "(Requires Drill)",
        Key = "Tab"
    };
    internal static PromptInfo HarvestHint = new PromptInfo()
    {
        Description = "Harvest",
        Key = "E"
    };
    internal static PromptInfo TerminalEnter = new PromptInfo()
    {
        Description = "Terminal",
        Key = "E"
    };
    internal static PromptInfo StopPowerPlugHint = new PromptInfo()
    {
        Description = "Stop running powerline",
        Key = "Esc"
    };
    internal static PromptInfo StopGasPipeHint = new PromptInfo()
    {
        Description = "Stop running pipeline",
        Key = "Esc"
    };
    internal static PromptInfo StartDrillHint = new PromptInfo()
    {
        Description = "Start Drill",
        Key = "E"
    };
    internal static PromptInfo DepositHint = new PromptInfo()
    {
        Description = "Deposit",
        Key = "",
        UsesProgress  = true
    };
}
