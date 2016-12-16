using UnityEngine;
using System.Collections;

/// <summary>
/// Information for things like "[E] Pick Up"
/// </summary>
public class PromptInfo
{
    public string Key { get; set; }
    public string Description { get; set; }
    public float Duration { get; set; }
}

public struct LinkablePrompts
{
    public PromptInfo HoverWhenNoneSelected;
    public PromptInfo HoverWhenOneSelected;
    public PromptInfo WhenCompleted;
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

    public static PromptInfo BulkheadBridgeCompletedPrompt = new PromptInfo()
    {
        Description = "Bulkheads connected",
        Key = "E",
        Duration = 1500
    };
    public static PromptInfo StartGasPipeHint = new PromptInfo()
    {
        Description = "Select gas valve to connect",
        Key = "E"
    };
    public static PromptInfo EndGasPipeHint = new PromptInfo()
    {
        Description = "Connect gas pipe",
        Key = "E"
    };
    public static PromptInfo GasPipeCompletedPrompt = new PromptInfo()
    {
        Description = "Gas Pipe connected",
        Key = "E",
        Duration = 1500
    };
    public static PromptInfo StartPowerPlugHint = new PromptInfo()
    {
        Description = "Select power socket to connect",
        Key = "E"
    };
    public static PromptInfo EndPowerPlugHint = new PromptInfo()
    {
        Description = "Connect power",
        Key = "E"
    };
    public static PromptInfo PowerPlugCompletedPrompt = new PromptInfo()
    {
        Description = "Power connected",
        Key = "E",
        Duration = 1500
    };
    public static PromptInfo DriveRoverPrompt = new PromptInfo()
    {
        Description = "Drive Rover",
        Key = "E"
    };
    public static PromptInfo PickupHint = new PromptInfo()
    {
        Description = "Pick up",
        Key = "E"
    };
    internal static PromptInfo DropHint = new PromptInfo()
    {
        Description = "Drop",
        Key = "E"
    };
    internal static PromptInfo ConstructHint = new PromptInfo()
    {
        Description = "Construct",
        Key = "E"
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
        Key = "E"
    };
    internal static LinkablePrompts BulkheadBridgePrompts = new LinkablePrompts()
    {
        HoverWhenNoneSelected = StartBulkheadBridgeHint,
        HoverWhenOneSelected = EndBulkheadBridgeHint,
        WhenCompleted = BulkheadBridgeCompletedPrompt
    };
    internal static LinkablePrompts GasPipePrompts = new LinkablePrompts()
    {
        HoverWhenNoneSelected = StartGasPipeHint,
        HoverWhenOneSelected = EndGasPipeHint,
        WhenCompleted = GasPipeCompletedPrompt
    };
    internal static LinkablePrompts PowerPlugPrompts = new LinkablePrompts()
    {
        HoverWhenNoneSelected = StartPowerPlugHint,
        HoverWhenOneSelected = EndPowerPlugHint,
        WhenCompleted = PowerPlugCompletedPrompt
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
}
