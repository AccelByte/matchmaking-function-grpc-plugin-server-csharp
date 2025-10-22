// Copyright (c) 2022-2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Text.Json.Serialization;

namespace AccelByte.PluginArch.Demo.Server.Model
{
    public class ValidationError : Exception
    {
        public ValidationError(string message) : base(message)
        {
        }
    }

    public class AllianceRule
    {
        [JsonPropertyName("min_number")]
        public int MinNumber { get; set; } = 0;

        [JsonPropertyName("max_number")]
        public int MaxNumber { get; set; } = 0;

        [JsonPropertyName("player_min_number")]
        public int PlayerMinNumber { get; set; } = 0;

        [JsonPropertyName("player_max_number")]
        public int PlayerMaxNumber { get; set; } = 0;
    }

    public class GameRules
    {
        [JsonPropertyName("shipCountMin")]
        public int ShipCountMin { get; set; } = 0;

        [JsonPropertyName("shipCountMax")]
        public int ShipCountMax { get; set; } = 0;

        [JsonPropertyName("auto_backfill")]
        public bool AutoBackfill { get; set; } = false;

        [JsonPropertyName("alliance")]
        public AllianceRule? Alliance { get; set; } = null;

        public void Validate()
        {
            if (Alliance == null)
                throw new ValidationError("alliance rule missing");
            
            if (Alliance.MinNumber > Alliance.MaxNumber)
                throw new ValidationError("alliance rule MaxNumber is less than MinNumber");
            
            if (Alliance.PlayerMinNumber > Alliance.PlayerMaxNumber)
                throw new ValidationError("alliance rule PlayerMaxNumber is less than PlayerMinNumber");
            
            if (ShipCountMin > ShipCountMax)
                throw new ValidationError("ShipCountMax is less than ShipCountMin");
        }
    }

    // Keep RuleObject for backward compatibility
    public class RuleObject
    {
        [JsonPropertyName("shipCountMin")]
        public int ShipCountMin { get; set; } = 0;

        [JsonPropertyName("shipCountMax")]
        public int ShipCountMax { get; set; } = 0;
    }
}
