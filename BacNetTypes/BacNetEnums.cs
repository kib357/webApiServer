namespace BacNetTypes
{
    public static class BacNetEnums
    {
        public const int BacnetMaxInstance = 0x3FFFFF;

        public const int BacnetProtocolVersion = 0x1;

        public const byte BacnetBvlcTypeBip = 0x81;
        public const int MaxBacnetPropertyId = 4194303;

        public static string GetErrorMessage(byte code)
        {
            if (code == 0x1f)
                return "Error: unknown-object";
            return "Error";
        }
    }
    #region BACNET_ABORT_REASON enum

    public enum BacnetAbortReason
    {
        Other = 0,
        BufferOverflow = 1,
        InvalidApduInThisState = 2,
        PreemptedByHigherPriorityTask = 3,
        SegmentationNotSupported = 4,
        /* Enumerated values 0-63 are reserved for definition by ASHRAE. */
        /* Enumerated values 64-65535 may be used by others subject to */
        /* the procedures and constraints described in Clause 23. */
        MaxBacnet = 5,
        FirstProprietary = 64,
        LastProprietary = 65535
    } 

    #endregion

    #region BACNET_ACKNOWLEDGMENT_FILTER enum

    public enum BacnetAcknowledgmentFilter
    {
        FilterAll = 0,
        FilterAcked = 1,
        FilterNotAcked = 2
    }

    #endregion

    #region BACNET_ACTION enum

    public enum BacnetAction
    {
        Direct = 0,
        Reverse = 1
    }

    #endregion

    #region BACNET_ACTION_VALUE_TYPE enum

    public enum BacnetActionValueType
    {
        BinaryPv,
        Unsigned,
        Float
    }

    #endregion

    #region BACNET_APPLICATION_TAG enum

    public enum BacnetApplicationTag
    {
        Null = 0,
        Boolean = 1,
        UnsignedInt = 2,
        SignedInt = 3,
        Real = 4,
        Double = 5,
        OctetString = 6,
        CharacterString = 7,
        BitString = 8,
        Enumerated = 9,
        Date = 10,
        Time = 11,
        ObjectId = 12,
        Reserve1 = 13,
        Reserve2 = 14,
        Reserve3 = 15,
        Max = 16
    }

    #endregion

    #region BACNET_BACNET_REJECT_REASON enum

    public enum BacnetBacnetRejectReason
    {
        Other = 0,
        BufferOverflow = 1,
        InconsistentParameters = 2,
        InvalidParameterDataType = 3,
        InvalidTag = 4,
        MissingRequiredParameter = 5,
        ParameterOutOfRange = 6,
        TooManyArguments = 7,
        UndefinedEnumeration = 8,
        UnrecognizedService = 9,
        /* Enumerated values 0-63 are reserved for definition by ASHRAE. */
        /* Enumerated values 64-65535 may be used by others subject to */
        /* the procedures and constraints described in Clause 23. */
        MaxBacnet = 10,
        FirstProprietary = 64,
        LastProprietary = 65535
    }

    #endregion

    #region BACNET_BINARY_PV enum

    public enum BacnetBinaryPv
    {
        MinBinaryPv = 0, /* for validating incoming values */
        BinaryInactive = 0,
        BinaryActive = 1,
        MaxBinaryPv = 1, /* for validating incoming values */
        BinaryNull = 2 /* our homemade way of storing this info */
    }

    #endregion

    #region BACNET_BVLC_FUNCTION enum

    public enum BacnetBvlcFunction
    {
        Result = 0,
        WriteBroadcastDistributionTable = 1,
        ReadBroadcastDistTable = 2,
        ReadBroadcastDistTableAck = 3,
        ForwardedNpdu = 4,
        RegisterForeignDevice = 5,
        ReadForeignDeviceTable = 6,
        ReadForeignDeviceTableAck = 7,
        DeleteForeignDeviceTableEntry = 8,
        DistributeBroadcastToNetwork = 9,
        OriginalUnicastNpdu = 10,
        OriginalBroadcastNpdu = 11,
        MaxFunction = 12
    }

    #endregion

    #region BACNET_BVLC_RESULT enum

    public enum BacnetBvlcResult
    {
        SuccessfulCompletion = 0x0000,
        WriteBroadcastDistributionTableNak = 0x0010,
        ReadBroadcastDistributionTableNak = 0x0020,
        RegisterForeignDeviceNak = 0X0030,
        ReadForeignDeviceTableNak = 0x0040,
        DeleteForeignDeviceTableEntryNak = 0x0050,
        DistributeBroadcastToNetworkNak = 0x0060
    }

    #endregion

    #region BACNET_CHARACTER_STRING_ENCODING enum

    public enum BacnetCharacterStringEncoding
    {
        AnsiX34 = 0,
        MsDbcs = 1,
        Jisc6226 = 2,
        Ucs4 = 3,
        Ucs2 = 4,
        Iso8859 = 5
    }

    #endregion

    #region BACNET_COMMUNICATION_ENABLE_DISABLE enum

    public enum BacnetCommunicationEnableDisable
    {
        Enable = 0,
        Disable = 1,
        DisableInitiation = 2,
        MaxBacnetCommunicationEnableDisable = 3
    }

    #endregion

    #region BACNET_CONFIRMED_SERVICE enum

    public enum BacnetConfirmedServices
    {
        /* Alarm and Event Services */
        AcknowledgeAlarm = 0,
        COVNotification = 1,
        EventNotification = 2,
        GetAlarmSummary = 3,
        GetEnrollmentSummary = 4,
        GetEventInformation = 29,
        SubscribeCOV = 5,
        SubscribeCOVProperty = 28,
        LifeSafetyOperation = 27,
        /* File Access Services */
        AtomicReadFile = 6,
        AtomicWriteFile = 7,
        /* Object Access Services */
        AddListElement = 8,
        RemoveListElement = 9,
        CreateObject = 10,
        DeleteObject = 11,
        ReadProperty = 12,
        ReadPropConditional = 13,
        ReadPropMultiple = 14,
        ReadRange = 26,
        WriteProperty = 15,
        WritePropMultiple = 16,
        /* Remote Device Management Services */
        DeviceCommunicationControl = 17,
        PrivateTransfer = 18,
        TextMessage = 19,
        ReinitializeDevice = 20,
        /* Virtual Terminal Services */
        VtOpen = 21,
        VtClose = 22,
        VtData = 23,
        /* Security Services */
        Authenticate = 24,
        RequestKey = 25,
        /* Services added after 1995 */
        /* readRange (26) see Object Access Services */
        /* lifeSafetyOperation (27) see Alarm and Event Services */
        /* subscribeCOVProperty (28) see Alarm and Event Services */
        /* getEventInformation (29) see Alarm and Event Services */
        MaxBacnetConfirmedService = 30
    }

    #endregion

    #region BACNET_DAYS_OF_WEEK enum

    public enum BacnetDaysOfWeek
    {
        Monday = 0,
        Tuesday = 1,
        Wednesday = 2,
        Thursday = 3,
        Friday = 4,
        Saturday = 5,
        Sunday = 6
    }

    #endregion

    #region BACNET_DEVICE_STATUS enum

    public enum BacnetDeviceStatus
    {
        Operational = 0,
        OperationalReadOnly = 1,
        DownloadRequired = 2,
        DownloadInProgress = 3,
        NonOperational = 4,
        MaxDevice = 5
    }

    #endregion

    #region BACNET_ENGINEERING_UNITS enum

    public enum BacnetEngineeringUnits
    {
        /* Acceleration */
        MetersPerSecondPerSecond = 166,
        /* Area */
        SquareMeters = 0,
        SquareCentimeters = 116,
        SquareFeet = 1,
        SquareInches = 115,
        /* Currency */
        Currency1 = 105,
        Currency2 = 106,
        Currency3 = 107,
        Currency4 = 108,
        Currency5 = 109,
        Currency6 = 110,
        Currency7 = 111,
        Currency8 = 112,
        Currency9 = 113,
        Currency10 = 114,
        /* Electrical */
        Milliamperes = 2,
        Amperes = 3,
        AmperesPerMeter = 167,
        AmperesPerSquareMeter = 168,
        AmpereSquareMeters = 169,
        Farads = 170,
        Henrys = 171,
        Ohms = 4,
        OhmMeters = 172,
        Milliohms = 145,
        Kilohms = 122,
        Megohms = 123,
        Siemens = 173, /* 1 mho equals 1 siemens */
        SiemensPerMeter = 174,
        Teslas = 175,
        Volts = 5,
        Millivolts = 124,
        Kilovolts = 6,
        Megavolts = 7,
        VoltAmperes = 8,
        KilovoltAmperes = 9,
        MegavoltAmperes = 10,
        VoltAmperesReactive = 11,
        KilovoltAmperesReactive = 12,
        MegavoltAmperesReactive = 13,
        VoltsPerDegreeKelvin = 176,
        VoltsPerMeter = 177,
        DegreesPhase = 14,
        PowerFactor = 15,
        Webers = 178,
        /* Energy */
        Joules = 16,
        Kilojoules = 17,
        KilojoulesPerKilogram = 125,
        Megajoules = 126,
        WattHours = 18,
        KilowattHours = 19,
        MegawattHours = 146,
        Btus = 20,
        KiloBtus = 147,
        MegaBtus = 148,
        Therms = 21,
        TonHours = 22,
        /* Enthalpy */
        JoulesPerKilogramDryAir = 23,
        KilojoulesPerKilogramDryAir = 149,
        MegajoulesPerKilogramDryAir = 150,
        BtusPerPoundDryAir = 24,
        BtusPerPound = 117,
        /* Entropy */
        JoulesPerDegreeKelvin = 127,
        KilojoulesPerDegreeKelvin = 151,
        MegajoulesPerDegreeKelvin = 152,
        JoulesPerKilogramDegreeKelvin = 128,
        /* Force */
        Newton = 153,
        /* Frequency */
        CyclesPerHour = 25,
        CyclesPerMinute = 26,
        Hertz = 27,
        Kilohertz = 129,
        Megahertz = 130,
        PerHour = 131,
        /* Humidity */
        GramsOfWaterPerKilogramDryAir = 28,
        PercentRelativeHumidity = 29,
        /* Length */
        Millimeters = 30,
        Centimeters = 118,
        Meters = 31,
        Inches = 32,
        Feet = 33,
        /* Light */
        Candelas = 179,
        CandelasPerSquareMeter = 180,
        WattsPerSquareFoot = 34,
        WattsPerSquareMeter = 35,
        Lumens = 36,
        Luxes = 37,
        FootCandles = 38,
        /* Mass */
        Kilograms = 39,
        PoundsMass = 40,
        Tons = 41,
        /* Mass Flow */
        GramsPerSecond = 154,
        GramsPerMinute = 155,
        KilogramsPerSecond = 42,
        KilogramsPerMinute = 43,
        KilogramsPerHour = 44,
        PoundsMassPerSecond = 119,
        PoundsMassPerMinute = 45,
        PoundsMassPerHour = 46,
        TonsPerHour = 156,
        /* Power */
        Milliwatts = 132,
        Watts = 47,
        Kilowatts = 48,
        Megawatts = 49,
        BtusPerHour = 50,
        KiloBtusPerHour = 157,
        Horsepower = 51,
        TonsRefrigeration = 52,
        /* Pressure */
        Pascals = 53,
        Hectopascals = 133,
        Kilopascals = 54,
        Millibars = 134,
        Bars = 55,
        PoundsForcePerSquareInch = 56,
        CentimetersOfWater = 57,
        InchesOfWater = 58,
        MillimetersOfMercury = 59,
        CentimetersOfMercury = 60,
        InchesOfMercury = 61,
        /* Temperature */
        DegreesCelsius = 62,
        DegreesKelvin = 63,
        DegreesKelvinPerHour = 181,
        DegreesKelvinPerMinute = 182,
        DegreesFahrenheit = 64,
        DegreeDaysCelsius = 65,
        DegreeDaysFahrenheit = 66,
        DeltaDegreesFahrenheit = 120,
        DeltaDegreesKelvin = 121,
        /* Time */
        Years = 67,
        Months = 68,
        Weeks = 69,
        Days = 70,
        Hours = 71,
        Minutes = 72,
        Seconds = 73,
        HundredthsSeconds = 158,
        Milliseconds = 159,
        /* Torque */
        NewtonMeters = 160,
        /* Velocity */
        MillimetersPerSecond = 161,
        MillimetersPerMinute = 162,
        MetersPerSecond = 74,
        MetersPerMinute = 163,
        MetersPerHour = 164,
        KilometersPerHour = 75,
        FeetPerSecond = 76,
        FeetPerMinute = 77,
        MilesPerHour = 78,
        /* Volume */
        CubicFeet = 79,
        CubicMeters = 80,
        ImperialGallons = 81,
        Liters = 82,
        UsGallons = 83,
        /* Volumetric Flow */
        CubicFeetPerSecond = 142,
        CubicFeetPerMinute = 84,
        CubicMetersPerSecond = 85,
        CubicMetersPerMinute = 165,
        CubicMetersPerHour = 135,
        ImperialGallonsPerMinute = 86,
        LitersPerSecond = 87,
        LitersPerMinute = 88,
        LitersPerHour = 136,
        UsGallonsPerMinute = 89,
        /* Other */
        DegreesAngular = 90,
        DegreesCelsiusPerHour = 91,
        DegreesCelsiusPerMinute = 92,
        DegreesFahrenheitPerHour = 93,
        DegreesFahrenheitPerMinute = 94,
        JouleSeconds = 183,
        KilogramsPerCubicMeter = 186,
        KwHoursPerSquareMeter = 137,
        KwHoursPerSquareFoot = 138,
        MegajoulesPerSquareMeter = 139,
        MegajoulesPerSquareFoot = 140,
        No = 95,
        NewtonSeconds = 187,
        NewtonsPerMeter = 188,
        PartsPerMillion = 96,
        PartsPerBillion = 97,
        Percent = 98,
        PercentObscurationPerFoot = 143,
        PercentObscurationPerMeter = 144,
        PercentPerSecond = 99,
        PerMinute = 100,
        PerSecond = 101,
        PsiPerDegreeFahrenheit = 102,
        Radians = 103,
        RadiansPerSecond = 184,
        RevolutionsPerMinute = 104,
        SquareMetersPerNewton = 185,
        WattsPerMeterPerDegreeKelvin = 189,
        WattsPerSquareMeterDegreeKelvin = 141
        /* Enumerated values 0-255 are reserved for definition by ASHRAE. */
        /* Enumerated values 256-65535 may be used by others subject to */
        /* the procedures and constraints described in Clause 23. */
        /* The last enumeration used in this version is 189. */
    }

    #endregion

    #region BACNET_ERROR_CLASS enum

    public enum BacnetErrorClass
    {
        Device = 0,
        Object = 1,
        Property = 2,
        Resources = 3,
        Security = 4,
        Services = 5,
        Vt = 6,
        /* Enumerated values 0-63 are reserved for definition by ASHRAE. */
        /* Enumerated values 64-65535 may be used by others subject to */
        /* the procedures and constraints described in Clause 23. */
        MaxBacnetErrorClass = 7,
        FirstProprietary = 64,
        LastProprietary = 65535
    }

    #endregion

    /* These are sorted in the order given in
           Clause 18. ERROR, REJECT AND ABORT CODES
           The Class and Code pairings are required
           to be used in accordance with Clause 18. */

    #region BACNET_ERROR_CODE enum

    public enum BacnetErrorCode
    {
        /* valid for all classes */
        Other = 0,

        /* Error Class - Device */
        DeviceBusy = 3,
        ConfigurationInProgress = 2,
        OperationalProblem = 25,

        /* Error Class - Object */
        DynamicCreationNotSupported = 4,
        NoObjectsOfSpecifiedType = 17,
        ObjectDeletionNotPermitted = 23,
        ObjectIdentifierAlreadyExists = 24,
        ReadAccessDenied = 27,
        UnknownObject = 31,
        UnsupportedObjectType = 36,

        /* Error Class - Property */
        CharacterSetNotSupported = 41,
        DatatypeNotSupported = 47,
        InconsistentSelectionCriterion = 8,
        InvalidArrayIndex = 42,
        InvalidDataType = 9,
        NotCOVProperty = 44,
        OptionalFunctionalityNotSupported = 45,
        PropertyIsNotAnArray = 50,
        /* ERROR_CODE_READ_ACCESS_DENIED = 27, */
        UnknownProperty = 32,
        ValueOutOfRange = 37,
        WriteAccessDenied = 40,

        /* Error Class - Resources */
        NoSpaceForObject = 18,
        NoSpaceToAddListElement = 19,
        NoSpaceToWriteProperty = 20,

        /* Error Class - Security */
        AuthenticationFailed = 1,
        /* ERROR_CODE_CHARACTER_SET_NOT_SUPPORTED = 41, */
        IncompatibleSecurityLevels = 6,
        InvalidOperatorName = 12,
        KeyGenerationError = 15,
        PasswordFailure = 26,
        SecurityNotSupported = 28,
        Timeout = 30,

        /* Error Class - Services */
        /* ERROR_CODE_CHARACTER_SET_NOT_SUPPORTED = 41, */
        COVSubscriptionFailed = 43,
        DuplicateName = 48,
        DuplicateObjectId = 49,
        FileAccessDenied = 5,
        InconsistentParameters = 7,
        InvalidConfigurationData = 46,
        InvalidFileAccessMethod = 10,
        InvalidFileStartPosition = 11,
        InvalidParameterDataType = 13,
        InvalidTimeStamp = 14,
        MissingRequiredParameter = 16,
        /* ERROR_CODE_OPTIONAL_FUNCTIONALITY_NOT_SUPPORTED = 45, */
        PropertyIsNotAList = 22,
        ServiceRequestDenied = 29,

        /* Error Class - VT */
        UnknownVtClass = 34,
        UnknownVtSession = 35,
        NoVtSessionsAvailable = 21,
        VtSessionAlreadyClosed = 38,
        VtSessionTerminationFailure = 39,

        /* unused */
        Reserved1 = 33,
        /* Enumerated values 0-255 are reserved for definition by ASHRAE. */
        /* Enumerated values 256-65535 may be used by others subject to */
        /* the procedures and constraints described in Clause 23. */
        /* The last enumeration used in this version is 50. */
        MaxBacnetErrorCode = 51,
        FirstProprietary = 256,
        LastProprietary = 65535
    }

    #endregion

    #region BACNET_EVENT_STATE enum

    public enum BacnetEventState
    {
        Normal = 0,
        Fault = 1,
        Offnormal = 2,
        HighLimit = 3,
        LowLimit = 4
    }

    #endregion

    #region BACNET_EVENT_STATE_FILTER enum

    public enum BacnetEventStateFilter
    {
        Offnormal = 0,
        Fault = 1,
        Normal = 2,
        All = 3,
        Active = 4
    }

    #endregion

    #region BACNET_EVENT_TYPE enum

    public enum BacnetEventType
    {
        ChangeOfBitstring = 0,
        ChangeOfState = 1,
        ChangeOfValue = 2,
        CommandFailure = 3,
        FloatingLimit = 4,
        OutOfRange = 5,
        /*  complex--type        (6), -- see comment below */
        /*  -buffer-ready   (7), -- context tag 7 is deprecated */
        ChangeOfLifeSafety = 8,
        Extended = 9,
        BufferReady = 10,
        UnsignedRange = 11
        /* Enumerated values 0-63 are reserved for definition by ASHRAE.  */
        /* Enumerated values 64-65535 may be used by others subject to  */
        /* the procedures and constraints described in Clause 23.  */
        /* It is expected that these enumerated values will correspond to  */
        /* the use of the complex--type CHOICE [6] of the  */
        /* BACnetNotificationParameters production. */
        /* The last enumeration used in this version is 11. */
    }

    #endregion

    #region BACNET_FILE_ACCESS_METHOD enum

    public enum BacnetFileAccessMethod
    {
        RecordAccess = 0,
        StreamAccess = 1,
        RecordAndStreamAccess = 2
    }

    #endregion

    #region BACNET_LIFE_SAFETY_MODE enum

    public enum BacnetLifeSafetyMode
    {
        Min = 0,
        Off = 0,
        On = 1,
        Test = 2,
        Manned = 3,
        Unmanned = 4,
        Armed = 5,
        Disarmed = 6,
        Prearmed = 7,
        Slow = 8,
        Fast = 9,
        Disconnected = 10,
        Enabled = 11,
        Disabled = 12,
        AutomaticReleaseDisabled = 13,
        Default = 14,
        Max = 14
        /* Enumerated values 0-255 are reserved for definition by ASHRAE.  */
        /* Enumerated values 256-65535 may be used by others subject to  */
        /* procedures and constraints described in Clause 23. */
    }

    #endregion

    #region BACNET_LIFE_SAFETY_OPERATION enum

    public enum BacnetLifeSafetyOperation
    {
        None = 0,
        Silence = 1,
        SilenceAudible = 2,
        SilenceVisual = 3,
        Reset = 4,
        ResetAlarm = 5,
        ResetFault = 6,
        Unsilence = 7,
        UnsilenceAudible = 8,
        UnsilenceVisual = 9
        /* Enumerated values 0-63 are reserved for definition by ASHRAE.  */
        /* Enumerated values 64-65535 may be used by others subject to  */
        /* procedures and constraints described in Clause 23. */
    }

    #endregion

    #region BACNET_LIFE_SAFETY_STATE enum

    public enum BacnetLifeSafetyState
    {
        Min = 0,
        Quiet = 0,
        PreAlarm = 1,
        Alarm = 2,
        Fault = 3,
        FaultPreAlarm = 4,
        FaultAlarm = 5,
        NotReady = 6,
        Active = 7,
        Tamper = 8,
        TestAlarm = 9,
        TestActive = 10,
        TestFault = 11,
        TestFaultAlarm = 12,
        Holdup = 13,
        Duress = 14,
        TamperAlarm = 15,
        Abnormal = 16,
        EmergencyPower = 17,
        Delayed = 18,
        Blocked = 19,
        LocalAlarm = 20,
        GeneralAlarm = 21,
        Supervisory = 22,
        TestSupervisory = 23,
        Max = 0
        /* Enumerated values 0-255 are reserved for definition by ASHRAE.  */
        /* Enumerated values 256-65535 may be used by others subject to  */
        /* procedures and constraints described in Clause 23. */
    }

    #endregion

    #region BACNET_LIGHTING_OPERATION enum

    public enum BacnetLightingOperation
    {
        Stop = 0,
        FadeTo = 1,
        FadeToOver = 2,
        RampTo = 3,
        RampToAtRate = 4,
        RampUp = 5,
        RampUpAtRate = 6,
        RampDown = 7,
        RampDownAtRate = 8,
        StepUp = 9,
        StepDown = 10,
        StepUpBy = 11,
        StepDownBy = 12,
        GotoLevel = 13,
        Relinquish = 14
    }

    #endregion

    #region BACNET_MAINTENANCE enum

    public enum BacnetMaintenance
    {
        None = 0,
        PeriodicTest = 1,
        AintenanceNeedServiceOperational = 2,
        NeedServiceInoperative = 3
        /* Enumerated values 0-255 are reserved for definition by ASHRAE.  */
        /* Enumerated values 256-65535 may be used by others subject to  */
        /* procedures and constraints described in Clause 23. */
    }

    #endregion

    #region BACNET_MESSAGE_PRIORITY enum

    public enum BacnetMessagePriority
    {
        Normal = 0,
        Urgent = 1,
        CriticalEquipment = 2,
        LifeSafety = 3
    }

    #endregion

    /*Network Layer Message Type */
    /*If Bit 7 of the control octet described in 6.2.2 is 1, */
    /* a message type octet shall be present as shown in Figure 6-1. */
    /* The following message types are indicated: */

    #region BACNET_NETWORK_MESSAGE_TYPE enum

    public enum BacnetNetworkMessageType
    {
        WhoIsRouterToNetwork = 0,
        IAmRouterToNetwork = 1,
        ICouldBeRouterToNetwork = 2,
        RejectMessageToNetwork = 3,
        RouterBusyToNetwork = 4,
        RouterAvailableToNetwork = 5,
        InitRtTable = 6,
        InitRtTableAck = 7,
        EstablishConnectionToNetwork = 8,
        DisconnectConnectionToNetwork = 9,
        /* X'0A' to X'7F': Reserved for use by ASHRAE, */
        /* X'80' to X'FF': Available for vendor proprietary messages */
        Invalid = 0x100
    }

    #endregion

    #region BACNET_NODE_TYPE enum

    public enum BacnetNodeType
    {
        Unknown = 0,
        System = 1,
        Network = 2,
        Device = 3,
        Organizational = 4,
        Area = 5,
        Equipment = 6,
        Point = 7,
        Collection = 8,
        Property = 9,
        Functional = 10,
        Other = 11
    }

    #endregion

    #region BACNET_NOTIFY_TYPE enum

    public enum BacnetNotifyType
    {
        Alarm = 0,
        Event = 1,
        AckNotification = 2
    }

    #endregion

    #region BACNET_OBJECT_TYPE enum

    public enum BacnetObjectType
    {
        AnalogInput = 0,
        AnalogOutput = 1,
        AnalogValue = 2,
        BinaryInput = 3,
        BinaryOutput = 4,
        BinaryValue = 5,
        Calendar = 6,
        Command = 7,
        Device = 8,
        EventEnrollment = 9,
        File = 10,
        Group = 11,
        Loop = 12,
        MultiStateInput = 13,
        MultiStateOutput = 14,
        NotificationClass = 15,
        Program = 16,
        Schedule = 17,
        Averaging = 18,
        MultiStateValue = 19,
        Trendlog = 20,
        LifeSafetyPoint = 21,
        LifeSafetyZone = 22,
        Accumulator = 23,
        PulseConverter = 24,
        EventLog = 25,
        GlobalGroup = 26,
        TrendLogMultiple = 27,
        LoadControl = 28,
        StructuredView = 29,
        /* what is object type 30? */
        LightingOutput = 31,
        /* Enumerated values 0-127 are reserved for definition by ASHRAE. */
        /* Enumerated values 128-1023 may be used by others subject to  */
        /* the procedures and constraints described in Clause 23. */
        MaxAshraeObjectType = 32, /* used for bit string loop */

        /*DELTA CONTROLS*/
        Door = 287,
        DoorGroup = 288,
        AccessControlEventLog = 297,

        MaxBacnetObjectType = 1023
    }

    #endregion

    /* note: these are not the real values, */
    /* but are shifted left for easy encoding */

    #region BACNET_PDU_TYPE enum

    public enum BacnetPduType
    {
        ConfirmedServiceRequest = 0,
        UnconfirmedServiceRequest = 0x10,
        SimpleAck = 0x20,
        ComplexAck = 0x30,
        SegmentAck = 0x40,
        Error = 0x50,
        Reject = 0x60,
        Abort = 0x70
    }

    #endregion

    #region BACNET_POLARITY enum

    public enum BacnetPolarity
    {
        Normal = 0,
        Reverse = 1
    }

    #endregion

    #region BACNET_PROGRAM_ERROR enum

    public enum BacnetProgramError
    {
        Normal = 0,
        LoadFailed = 1,
        Internal = 2,
        Program = 3,
        Other = 4
        /* Enumerated values 0-63 are reserved for definition by ASHRAE.  */
        /* Enumerated values 64-65535 may be used by others subject to  */
        /* the procedures and constraints described in Clause 23. */
    }

    #endregion

    #region BACNET_PROGRAM_REQUEST enum

    public enum BacnetProgramRequest
    {
        Ready = 0,
        Load = 1,
        Run = 2,
        Halt = 3,
        Restart = 4,
        Unload = 5
    }

    #endregion

    #region BACNET_PROGRAM_STATE enum

    public enum BacnetProgramState
    {
        Idle = 0,
        Loading = 1,
        Running = 2,
        Waiting = 3,
        Halted = 4,
        Unloading = 5
    }

    #endregion

    #region BACNET_REINITIALIZED_STATE enum

    public enum BacnetReinitializedState
    {
        Coldstart = 0,
        Warmstart = 1,
        Startbackup = 2,
        Endbackup = 3,
        Startrestore = 4,
        Endrestore = 5,
        Abortrestore = 6,
        MaxBacnetReinitializedState = 7
    }

    #endregion

    #region BACNET_REINITIALIZED_STATE_OF_DEVICE enum

    public enum BacnetReinitializedStateOfDevice
    {
        ColdStart = 0,
        WarmStart = 1,
        StartBackup = 2,
        EndBackup = 3,
        StartRestore = 4,
        EndRestore = 5,
        AbortRestore = 6,
        Idle = 255
    }

    #endregion

    #region BACNET_RELATION_SPECIFIER enum

    public enum BacnetRelationSpecifier
    {
        Equal = 0,
        NotEqual = 1,
        LessThan = 2,
        GreaterThan = 3,
        LessThanOrEqual = 4,
        GreaterThanOrEqual = 5
    }

    #endregion

    #region BACNET_RELIABILITY enum

    public enum BacnetReliability
    {
        NoFaultDetected = 0,
        NoSensor = 1,
        OverRange = 2,
        UnderRange = 3,
        OpenLoop = 4,
        ShortedLoop = 5,
        NoOutput = 6,
        UnreliableOther = 7,
        ProcessError = 8,
        MultiStateFault = 9,
        ConfigurationError = 10,
        CommunicationFailure = 12,
        Tripped = 13
        /* Enumerated values 0-63 are reserved for definition by ASHRAE.  */
        /* Enumerated values 64-65535 may be used by others subject to  */
        /* the procedures and constraints described in Clause 23. */
    }

    #endregion

    #region BACNET_SEGMENTATION enum

    public enum BacnetSegmentation
    {
        Both = 0,
        Transmit = 1,
        Receive = 2,
        None = 3,
        MaxBacnetSegmentation = 4
    }

    #endregion

    #region BACNET_SELECTION_LOGIC enum

    public enum BacnetSelectionLogic
    {
        And = 0,
        Or = 1,
        All = 2
    }

    #endregion

    #region BACNET_SERVICES_SUPPORTED enum

    public enum BacnetServicesSupported
    {
        /* Alarm and Event Services */
        AcknowledgeAlarm = 0,
        ConfirmedCOVNotification = 1,
        ConfirmedEventNotification = 2,
        GetAlarmSummary = 3,
        GetEnrollmentSummary = 4,
        GetEventInformation = 39,
        SubscribeCOV = 5,
        SubscribeCOVProperty = 38,
        LifeSafetyOperation = 37,
        /* File Access Services */
        AtomicReadFile = 6,
        AtomicWriteFile = 7,
        /* Object Access Services */
        AddListElement = 8,
        RemoveListElement = 9,
        CreateObject = 10,
        DeleteObject = 11,
        ReadProperty = 12,
        ReadPropConditional = 13,
        ReadPropMultiple = 14,
        ReadRange = 35,
        WriteProperty = 15,
        WritePropMultiple = 16,
        /* Remote Device Management Services */
        DeviceCommunicationControl = 17,
        PrivateTransfer = 18,
        TextMessage = 19,
        ReinitializeDevice = 20,
        /* Virtual Terminal Services */
        VtOpen = 21,
        VtClose = 22,
        VtData = 23,
        /* Security Services */
        Authenticate = 24,
        RequestKey = 25,
        IAm = 26,
        IHave = 27,
        UnconfirmedCOVNotification = 28,
        UnconfirmedEventNotification = 29,
        UnconfirmedPrivateTransfer = 30,
        UnconfirmedTextMessage = 31,
        TimeSynchronization = 32,
        UtcTimeSynchronization = 36,
        WhoHas = 33,
        WhoIs = 34,
        /* Other services to be added as they are defined. */
        /* All values in this production are reserved */
        /* for definition by ASHRAE. */
        MaxBacnetServicesSupported = 40
    }

    #endregion

    #region BACNET_SHED_STATE enum

    public enum BacnetShedState
    {
        Inactive = 0,
        RequestPending = 1,
        Compliant = 2,
        NonCompliant = 3
    }

    #endregion

    #region BACNET_SILENCED_STATE enum

    public enum BacnetSilencedState
    {
        Unsilenced = 0,
        AudibleSilenced = 1,
        VisibleSilenced = 2,
        AllSilenced = 3
        /* Enumerated values 0-63 are reserved for definition by ASHRAE. */
        /* Enumerated values 64-65535 may be used by others subject to */
        /* procedures and constraints described in Clause 23. */
    }

    #endregion

    #region BACNET_STATUS_FLAGS enum

    public enum BacnetStatusFlags
    {
        InAlarm = 0,
        Fault = 1,
        Overridden = 2,
        OutOfService = 3
    }

    #endregion

    #region BACNET_UNCONFIRMED_SERVICE enum

    public enum BacnetUnconfirmedService
    {
        IAm = 0,
        IHave = 1,
        COVNotification = 2,
        EventNotification = 3,
        PrivateTransfer = 4,
        TextMessage = 5,
        TimeSynchronization = 6,
        WhoHas = 7,
        WhoIs = 8,
        UtcTimeSynchronization = 9,
        /* Other services to be added as they are defined. */
        /* All choice values in this production are reserved */
        /* for definition by ASHRAE. */
        /* Proprietary extensions are made by using the */
        /* UnconfirmedPrivateTransfer service. See Clause 23. */
        MaxBacnetUnconfirmedService = 10
    }

    #endregion

    #region BACNET_VT_CLASS enum

    public enum BacnetVtClass
    {
        Default = 0,
        AnsiX34 = 1, /* real name is ANSI X3.64 */
        DecVt52 = 2,
        DecVt100 = 3,
        DecVt220 = 4,
        Hp70094 = 5, /* real name is HP 700/94 */
        Ibm3130 = 6
        /* Enumerated values 0-63 are reserved for definition by ASHRAE.  */
        /* Enumerated values 64-65535 may be used by others subject to  */
        /* the procedures and constraints described in Clause 23. */
    }

    #endregion

    #region BacnetPropertyId enum

    public enum BacnetPropertyId
    {
        AckedTransitions = 0,
        AckRequired = 1,
        Action = 2,
        ActionText = 3,
        ActiveText = 4,
        ActiveVtSessions = 5,
        AlarmValue = 6,
        AlarmValues = 7,
        All = 8,
        AllWritesSuccessful = 9,
        ApduSegmentTimeout = 10,
        ApduTimeout = 11,
        ApplicationSoftwareVersion = 12,
        Archive = 13,
        Bias = 14,
        ChangeOfStateCount = 15,
        ChangeOfStateTime = 16,
        NotificationClass = 17,
        Blank1 = 18,
        ControlledVariableReference = 19,
        ControlledVariableUnits = 20,
        ControlledVariableValue = 21,
        COVIncrement = 22,
        DateList = 23,
        DaylightSavingsStatus = 24,
        Deadband = 25,
        DerivativeConstant = 26,
        DerivativeConstantUnits = 27,
        Description = 28,
        DescriptionOfHalt = 29,
        DeviceAddressBinding = 30,
        DeviceType = 31,
        EffectivePeriod = 32,
        ElapsedActiveTime = 33,
        ErrorLimit = 34,
        EventEnable = 35,
        EventState = 36,
        EventType = 37,
        ExceptionSchedule = 38,
        FaultValues = 39,
        FeedbackValue = 40,
        FileAccessMethod = 41,
        FileSize = 42,
        FileType = 43,
        FirmwareRevision = 44,
        HighLimit = 45,
        InactiveText = 46,
        InProcess = 47,
        InstanceOf = 48,
        IntegralConstant = 49,
        IntegralConstantUnits = 50,
        IssueConfirmedNotifications = 51,
        LimitEnable = 52,
        ListOfGroupMembers = 53,
        ListOfObjectPropertyReferences = 54,
        ListOfSessionKeys = 55,
        LocalDate = 56,
        LocalTime = 57,
        Location = 58,
        LowLimit = 59,
        ManipulatedVariableReference = 60,
        MaximumOutput = 61,
        MaxApduLengthAccepted = 62,
        MaxInfoFrames = 63,
        MaxMaster = 64,
        MaxPresValue = 65,
        MinimumOffTime = 66,
        MinimumOnTime = 67,
        MinimumOutput = 68,
        MinPresValue = 69,
        ModelName = 70,
        ModificationDate = 71,
        NotifyType = 72,
        NumberOfApduRetries = 73,
        NumberOfStates = 74,
        ObjectIdentifier = 75,
        ObjectList = 76,
        ObjectName = 77,
        ObjectertyReference = 78,
        ObjectType = 79,
        Optional = 80,
        OutOfService = 81,
        OutputUnits = 82,
        EventParameters = 83,
        Polarity = 84,
        PresentValue = 85,
        Priority = 86,
        PriorityArray = 87,
        PriorityForWriting = 88,
        ProcessIdentifier = 89,
        ProgramChange = 90,
        ProgramLocation = 91,
        ProgramState = 92,
        ProportionalConstant = 93,
        ProportionalConstantUnits = 94,
        ProtocolConformanceClass = 95, /* deleted in version 1 revision 2 */
        ProtocolObjectTypesSupported = 96,
        ProtocolServicesSupported = 97,
        ProtocolVersion = 98,
        ReadOnly = 99,
        ReasonForHalt = 100,
        Recipient = 101,
        RecipientList = 102,
        Reliability = 103,
        RelinquishDefault = 104,
        Required = 105,
        Resolution = 106,
        SegmentationSupported = 107,
        Setpoint = 108,
        SetpointReference = 109,
        StateText = 110,
        StatusFlags = 111,
        SystemStatus = 112,
        TimeDelay = 113,
        TimeOfActiveTimeReset = 114,
        TimeOfStateCountReset = 115,
        TimeSynchronizationRecipients = 116,
        Units = 117,
        UpdateInterval = 118,
        UtcOffset = 119,
        VendorIdentifier = 120,
        VendorName = 121,
        VtClassesSupported = 122,
        WeeklySchedule = 123,
        AttemptedSamples = 124,
        AverageValue = 125,
        BufferSize = 126,
        ClientCOVIncrement = 127,
        COVResubscriptionInterval = 128,
        CurrentNotifyTime = 129,
        EventTimeStamps = 130,
        LogBuffer = 131,
        LogDeviceObject = 132,
        /* The enable erty is renamed from log-enable in
           Addendum b to ANSI/ASHRAE 135-2004(135b-2) */
        Enable = 133,
        LogInterval = 134,
        MaximumValue = 135,
        MinimumValue = 136,
        NotificationThreshold = 137,
        PreviousNotifyTime = 138,
        ProtocolRevision = 139,
        RecordsSinceNotification = 140,
        RecordCount = 141,
        StartTime = 142,
        StopTime = 143,
        StopWhenFull = 144,
        TotalRecordCount = 145,
        ValidSamples = 146,
        WindowInterval = 147,
        WindowSamples = 148,
        MaximumValueTimestamp = 149,
        MinimumValueTimestamp = 150,
        VarianceValue = 151,
        ActiveCOVSubscriptions = 152,
        BackupFailureTimeout = 153,
        ConfigurationFiles = 154,
        DatabaseRevision = 155,
        DirectReading = 156,
        LastRestoreTime = 157,
        MaintenanceRequired = 158,
        MemberOf = 159,
        Mode = 160,
        OperationExpected = 161,
        Setting = 162,
        Silenced = 163,
        TrackingValue = 164,
        ZoneMembers = 165,
        LifeSafetyAlarmValues = 166,
        MaxSegmentsAccepted = 167,
        ProfileName = 168,
        AutoSlaveDiscovery = 169,
        ManualSlaveAddressBinding = 170,
        SlaveAddressBinding = 171,
        SlaveProxyEnable = 172,
        LastNotifyTime = 173,
        ScheduleDefault = 174,
        AcceptedModes = 175,
        AdjustValue = 176,
        Count = 177,
        CountBeforeChange = 178,
        CountChangeTime = 179,
        COVPeriod = 180,
        InputReference = 181,
        LimitMonitoringInterval = 182,
        LoggingDevice = 183,
        LoggingRecord = 184,
        Prescale = 185,
        PulseRate = 186,
        Scale = 187,
        ScaleFactor = 188,
        UpdateTime = 189,
        ValueBeforeChange = 190,
        ValueSet = 191,
        ValueChangeTime = 192,
        /* enumerations 193-206 are new */
        AlignIntervals = 193,
        GroupMemberNames = 194,
        IntervalOffset = 195,
        LastRestartReason = 196,
        LoggingType = 197,
        MemberStatusFlags = 198,
        NotificationPeriod = 199,
        PreviousNotifyRecord = 200,
        RequestedUpdateInterval = 201,
        RestartNotificationRecipients = 202,
        TimeOfDeviceRestart = 203,
        TimeSynchronizationInterval = 204,
        Trigger = 205,
        UtcTimeSynchronizationRecipients = 206,
        /* enumerations 207-211 are used in Addendum d to ANSI/ASHRAE 135-2004 */
        NodeSubtype = 207,
        NodeType = 208,
        StructuredObjectList = 209,
        SubordinateAnnotations = 210,
        SubordinateList = 211,
        /* enumerations 212-225 are used in Addendum e to ANSI/ASHRAE 135-2004 */
        ActualShedLevel = 212,
        DutyWindow = 213,
        ExpectedShedLevel = 214,
        FullDutyBaseline = 215,
        /* enumerations 216-217 are used in Addendum i to ANSI/ASHRAE 135-2004 */
        BlinkPriorityThreshold = 216,
        BlinkTime = 217,
        /* enumerations 212-225 are used in Addendum e to ANSI/ASHRAE 135-2004 */
        RequestedShedLevel = 218,
        ShedDuration = 219,
        ShedLevelDescriptions = 220,
        ShedLevels = 221,
        StateDescription = 222,
        /* enumerations 223-225 are used in Addendum i to ANSI/ASHRAE 135-2004 */
        FadeTime = 223,
        LightingCommand = 224,
        LightingCommandPriority = 225,
        /* enumerations 226-235 are used in Addendum f to ANSI/ASHRAE 135-2004 */
        /* enumerations 236-243 are used in Addendum i to ANSI/ASHRAE 135-2004 */
        OffDelay = 236,
        OnDelay = 237,
        Power = 238,
        PowerOnValue = 239,
        ProgressValue = 240,
        RampRate = 241,
        StepIncrement = 242,
        SystemFailureValue = 243
        /* The special property identifiers all, optional, and required  */
        /* are reserved for use in the ReadPropertyConditional and */
        /* ReadPropertyMultiple services or services not defined in this standard. */
        /* Enumerated values 0-511 are reserved for definition by ASHRAE.  */
        /* Enumerated values 512-4194303 may be used by others subject to the  */
        /* procedures and constraints described in Clause 23.  */
    }

    #endregion    
}