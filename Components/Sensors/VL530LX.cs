using System;
using Scarlet.Utilities;
using Scarlet.IO;

namespace Scarlet.Components.Sensors
{
	public class VL530LX : ISensor
	{
		public enum Error : sbyte
		{
			None = 0,
			CalibrationWarning = -1,
			MinClipped = -2,
			Undefined = -3,
			InvalidParams = -4,
			NotSupported = -5,
			ErrorRange = -6,
			TimeOut = -7,
			ModeNotSupported = -8,
			BufferTooSmall = -9,
			GPIONotExisting = -10,
			GPIOFuncitonalityNotSupported = -11,
			InterruptNotCleared = -12,
			ControlInterface = -20,
			InvalidCommand = -30,
			DivisionByZero = -40,
			RefSpadInit = -50,
			NotImplemented = -99,
		}

		public enum DeviceError : byte
		{
			None = 0,
			VCSelContinuityTestFailure = 1,
			VCSelWatchdogTestFailure = 2,
			NoVHVValueFound = 3,
			MSRCNotTarget = 4,
			SNRCheck = 5,
			RangePHaseCheck = 6,
			SigmaThresholdCheck = 7,
			TCC = 9,
			PhaseConsistency = 9,
			MinClip = 10,
			RangeComplete = 11,
			AlgoUnderFlow = 12,
			AlgoOverFlow = 13,
			RangeIgnoreThreshold = 14,
		}

		public enum PowerModes : byte
		{
			StandbyLevel1 = 0,
			StandbyLevel2 = 1,
			IdleLevel1 = 2,
			IdleLevel2 = 3,
		}

		public enum State : byte
		{
			Powerdown = 0,
			WaitStaticInit = 1,
			Standby = 2,
			Idle = 3,
			Running = 4,
			Unknown = 98,
			Error = 99,
		}

		public enum DeviceModes : int
		{
			SingleRanging = 0,
			ContinuousRanging = 1,
			SingleHistogram = 2,
			ContinuousTimedRanging = 3,
			SingleALS = 10,
			GpioDrive = 20,
			GpioOSC = 21,
		}

		public enum HistogramModes : byte
		{
			Disabled = 0,
			ReferenceOnly = 1,
			ReturnOnly = 2,
			Both = 3,
		}

		public enum CheckEnable : int
		{
			SigmaFinalRange,
			SignalRateFinalRange,
			SignalRefClip,
			RangeIgnoreThreshold,
			SignalRateMSRC,
			SignalRatePreRange,
			NumberOfChecks,
		}

		public enum GpioFunctionality : byte
		{
			Off = 0,
			ThresholdCrossedLow = 1,
			ThresholdCrossedHigh = 2,
			ThresholdCrossedOut = 3,
			NewMeasureReady = 4,
		}

		public struct RangingMeasurementData
		{
			public uint TimeStamp;
			public uint MeasurementTimeUsec;
			public ushort RangeMilliMeter;
			public ushort RangeDMaxMilliMeter;
			public uint SingalRangeRtnMegaCps;
			public uint AmbientRateRtnMegaCps;
			public uint EffectiveSpadRtnCount;
			public byte ZoneId;
			public byte RangeFractionalPart;
			public byte RangeStatus;
		}

		public struct DMaxDataT
		{
			int AmbTuningWindowFactor_K;
			int RetSignalAt0mm;
		}

		public unsafe struct DeviceParameters
		{
			public DeviceModes DeviceMode;
			/*!< Defines type of measurement to be done for the next measure */
			public HistogramModes HistogramMode;
			/*!< Defines type of histogram measurement to be done for the next
             *      measure */
			public uint MeasurementTimingBudgetMicroSeconds;
			/*!< Defines the allowed total time for a single measurement */
			public uint InterMeasurementPeriodMilliSeconds;
			/*!< Defines time between two consecutive measurements (between two
             *      measurement starts). If set to 0 means back-to-back mode */
			public byte XTalkCompensationEnable;
			/*!< Tells if Crosstalk compensation shall be enable or not      */
			public ushort XTalkCompensationRangeMilliMeter;
			/*!< CrossTalk compensation range in millimeter  */
			public uint XTalkCompensationRateMegaCps;
			/*!< CrossTalk compensation rate in Mega counts per seconds.
             *      Expressed in 16.16 fixed point format.  */
			public int RangeOffsetMicroMeters;
			/*!< Range offset adjustment (mm).      */

			public fixed byte LimitChecksEnable[(int)CheckEnable.NumberOfChecks];
			/*!< This Array store all the Limit Check enable for this device. */
			public fixed byte LimitChecksStatus[(int)CheckEnable.NumberOfChecks];
			/*!< This Array store all the Status of the check linked to last
            * measurement. */
			public fixed uint LimitChecksValue[(int)CheckEnable.NumberOfChecks];
			/*!< This Array store all the Limit Check value for this device */

			public byte WrapAroundCheckEnable;
			/*!< Tells if Wrap Around Check shall be enable or not */
		}

		public unsafe struct HistogramMeasurementData
		{
			public const int HISTOGRAM_BUFFER_SIZE = 24;
			/* Histogram Measurement data */
			public fixed uint HistogramData[HISTOGRAM_BUFFER_SIZE];
			/*!< Histogram data */
			public byte HistogramType; /*!< Indicate the types of histogram data :
        Return only, Reference only, both Return and Reference */
			public byte FirstBin; /*!< First Bin value */
			public byte BufferSize; /*!< Buffer Size - Set by the user.*/
			public byte NumberOfBins;
			/*!< Number of bins filled by the histogram measurement */

			public DeviceError ErrorStatus;
			/*!< Error status of the current measurement. \n
            see @a ::VL53L0X_DeviceError @a VL53L0X_GetStatusErrorString() */
		}

		public struct DeviceSpecificParameters
		{
			public uint OscFrequencyMHz; /* Frequency used */

			public ushort LastEncodedTimeout;
			/* last encoded Time out used for timing budget*/

			public GpioFunctionality Pin0GpioFunctionality;
			/* store the functionality of the GPIO: pin0 */

			public uint FinalRangeTimeoutMicroSecs;
			/*!< Execution time of the final range*/
			public byte FinalRangeVcselPulsePeriod;
			/*!< Vcsel pulse period (pll clocks) for the final range measurement*/
			public int PreRangeTimeoutMicroSecs;
			/*!< Execution time of the final range*/
			public byte PreRangeVcselPulsePeriod;
			/*!< Vcsel pulse period (pll clocks) for the pre-range measurement*/

			public ushort SigmaEstRefArray;
			/*!< Reference array sigma value in 1/100th of [mm] e.g. 100 = 1mm */
			public ushort SigmaEstEffPulseWidth;
			/*!< Effective Pulse width for sigma estimate in 1/100th
             * of ns e.g. 900 = 9.0ns */
			public ushort SigmaEstEffAmbWidth;
			/*!< Effective Ambient width for sigma estimate in 1/100th of ns
             * e.g. 500 = 5.0ns */


			public byte ReadDataFromDeviceDone; /* Indicate if read from device has
        been done (==1) or not (==0) */
			public byte ModuleId; /* Module ID */
			public byte Revision; /* test Revision */
			public string ProductId;
			/* Product Identifier String  */
			public byte ReferenceSpadCount; /* used for ref spad management */
			public byte ReferenceSpadType;      /* used for ref spad management */
			public byte RefSpadsInitialised; /* reports if ref spads are initialised. */
			public int PartUIDUpper; /*!< Unique Part ID Upper */
			public int PartUIDLower; /*!< Unique Part ID Lower */
			public uint SignalRateMeasFixed400mm; /*!< Peek Signal rate */
		}

		public unsafe struct SpadDataT
		{
			public const int SPAD_BUFFER_SIZE = 6;
			public fixed byte RefSpadEnables[SPAD_BUFFER_SIZE];
			/*!< Reference Spad Enables */
			public fixed byte RefGoodSpadMap[SPAD_BUFFER_SIZE];
			/*!< Reference Spad Good Spad Map */
		}

		public struct DeviceData
		{
			public DMaxDataT DMaxData;
			/*!< Dmax Data */
			public int Part2PartOffsetNVMMicroMeter;
			/*!< backed up NVM value */
			public int Part2PartOffsetAdjustmentNVMMicroMeter;
			/*!< backed up NVM value representing additional offset adjustment */
			public DeviceParameters CurrentParameters;
			/*!< Current Device Parameter */
			public RangingMeasurementData LastRangeMeasure;
			/*!< Ranging Data */
			public HistogramMeasurementData LastHistogramMeasure;
			/*!< Histogram Data */
			public DeviceSpecificParameters DeviceSpecificParameters;
			/*!< Parameters specific to the device */
			public SpadDataT SpadData;
			/*!< Spad Data */
			public byte SequenceConfig;
			/*!< Internal value for the sequence config */
			public byte RangeFractionalEnable;
			/*!< Enable/Disable fractional part of ranging data */
			public State PalState;
			/*!< Current state of the PAL for this device */
			public PowerModes PowerMode;
			/*!< Current Power Mode  */
			public ushort SigmaEstRefArray;
			/*!< Reference array sigma value in 1/100th of [mm] e.g. 100 = 1mm */
			public ushort SigmaEstEffPulseWidth;
			/*!< Effective Pulse width for sigma estimate in 1/100th
            * of ns e.g. 900 = 9.0ns */
			public ushort SigmaEstEffAmbWidth;
			/*!< Effective Ambient width for sigma estimate in 1/100th of ns
            * e.g. 500 = 5.0ns */
			public byte StopVariable;
			/*!< StopVariable used during the stop sequence */
			public ushort targetRefRate;
			/*!< Target Ambient Rate for Ref spad management */
			public uint SigmaEstimate;
			/*!< Sigma Estimate - based on ambient & VCSEL rates and
            * signal_total_events */
			public uint SignalEstimate;
			/*!< Signal Estimate - based on ambient & VCSEL rates and cross talk */
			public uint LastSignalRefMcps;
			/*!< Latest Signal ref in Mcps */
			public IntPtr pTuningSettingsPointer;
			/*!< Pointer for Tuning Settings table */
			public byte UseInternalTuningSettings;
			/*!< Indicate if we use  Tuning Settings table */
			public ushort LinearityCorrectiveGain;
			/*!< Linearity Corrective Gain value in x1000 */
			public ushort DmaxCalRangeMilliMeter;
			/*!< Dmax Calibration Range millimeter */
			public uint DmaxCalSignalRateRtnMegaCps;
			/*!< Dmax Calibration Signal Rate Return MegaCps */
		}

		//private RangingMeasurementData Data;
		private DeviceData Dev;
		private II2CBus Bus;

		public VL530LX(II2CBus I2C)
		{
			Bus = I2C;
		}

		public string System { get; set; }

		public void EventTriggered(object Sender, EventArgs Event)
		{
			throw new NotImplementedException();
		}

		private static ushort MakeUint16(byte LSB, byte MSB)
		{
			return (ushort)(MSB << 8 | LSB);
		}

		private static uint FixPoint97To1616(ushort x)
		{
			return (uint)(x << 9);
		}

		public DataUnit GetData()
		{
			RangingMeasurementData NewData = new RangingMeasurementData();
			RangingMeasurementData OldData = new RangingMeasurementData();
			byte[] Buffer = Bus.Read(0x14, 12);
			NewData.ZoneId = 0;
			NewData.TimeStamp = 0;
			NewData.MeasurementTimeUsec = 0;
			ushort Temp = MakeUint16(Buffer[10], Buffer[11]);
			uint SignalRate = FixPoint97To1616(MakeUint16(Buffer[7], Buffer[6]));
			NewData.AmbientRateRtnMegaCps = SignalRate;
			ushort AmbientRate = MakeUint16(Buffer[9], Buffer[8]);
			NewData.AmbientRateRtnMegaCps = FixPoint97To1616(AmbientRate);
			ushort EffectiveSpadRtnCount = MakeUint16(Buffer[3], Buffer[2]);
			NewData.EffectiveSpadRtnCount = EffectiveSpadRtnCount;

			byte DeviceRangeStatus = Buffer[0];
			ushort LinearityCorrectiveGain = Dev.LinearityCorrectiveGain;
			byte RangeFractionalEnable = Dev.RangeFractionalEnable;
			ushort XtalkRangeMilliMeter;
			if (LinearityCorrectiveGain != 1000)
			{
				Temp = (ushort)((LinearityCorrectiveGain * Temp + 500) / 1000);
				ushort XTalkCompensationRateMegaCps = (ushort)Dev.CurrentParameters.XTalkCompensationRateMegaCps;
				byte XTalkCompensationEnable = Dev.CurrentParameters.XTalkCompensationEnable;
				if (XTalkCompensationEnable != 0)
				{
					if ((SignalRate -
					   ((XTalkCompensationRateMegaCps
						 * EffectiveSpadRtnCount) >> 8)) <= 0)
					{
						XtalkRangeMilliMeter = 8888;
						if (RangeFractionalEnable == 0) { XtalkRangeMilliMeter <<= 2; }
					}
					else
					{
						XtalkRangeMilliMeter = (ushort)
							((Temp * SignalRate)
							/ (SignalRate
							   - ((XTalkCompensationRateMegaCps
								   * EffectiveSpadRtnCount)
								  >> 8)));
					}
					Temp = XtalkRangeMilliMeter;
				}
			}

			if (RangeFractionalEnable != 0)
			{
				NewData.RangeMilliMeter = (ushort)(Temp >> 2);
				NewData.RangeFractionalPart = (byte)((Temp & 0x03) << 6);
			}
			else
			{
				NewData.RangeMilliMeter = Temp;
				NewData.RangeFractionalPart = 0;
			}
			OldData = Dev.LastRangeMeasure;
			OldData.RangeMilliMeter = NewData.RangeMilliMeter;
			OldData.RangeFractionalPart = NewData.RangeFractionalPart;
			OldData.RangeDMaxMilliMeter = NewData.RangeDMaxMilliMeter;
			OldData.MeasurementTimeUsec = NewData.MeasurementTimeUsec;
			OldData.SingalRangeRtnMegaCps = NewData.SingalRangeRtnMegaCps;
			OldData.AmbientRateRtnMegaCps = NewData.AmbientRateRtnMegaCps;
			OldData.EffectiveSpadRtnCount = NewData.EffectiveSpadRtnCount;
			OldData.RangeStatus = NewData.RangeStatus;

			Dev.LastRangeMeasure = OldData;

			DataUnit Result = new DataUnit("VL530LX");
			Result.Add("Range", NewData.RangeMilliMeter);
			return Result;
		}

		public bool Test()
		{
			throw new NotImplementedException();
		}

		public void UpdateState()
		{
			throw new NotImplementedException();
		}


	}
}
