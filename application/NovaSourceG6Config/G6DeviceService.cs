using System.Globalization;

namespace NovaSourceG6Config;

public sealed record G6DeviceState(
    decimal FrequencyMhz,
    decimal MinimumFrequencyMhz,
    decimal MaximumFrequencyMhz,
    int Attenuation,
    string InputMode,
    string TriggerMode,
    bool InternalTriggerEnabled,
    bool RfStandbyEnabled,
    string ModulationSource,
    int ModulationGain,
    bool RfOn,
    bool Locked,
    bool PowerOn);

public sealed class G6DeviceService(SerialPortProbeService serial)
{
    public async Task<G6DeviceState> ReadStateAsync(CancellationToken cancellationToken = default)
    {
        var minimum = await QueryDecimalAsync("LF", cancellationToken);
        var maximum = await QueryDecimalAsync("HF", cancellationToken);
        var frequency = await QueryDecimalAsync("FR", cancellationToken);
        var attenuation = await QueryIntAsync("AT", cancellationToken);
        var inputMode = await QueryAsync("IM", cancellationToken);
        var triggerMode = await QueryAsync("TM", cancellationToken);
        var internalTrigger = await QueryAsync("IT", cancellationToken);
        var standby = await QueryAsync("RS", cancellationToken);
        var modulation = await QueryAsync("MS", cancellationToken);
        var gain = await QueryIntAsync("MG", cancellationToken);
        var led = await QueryIntAsync("LS", cancellationToken);

        if (led is not (1 or 3 or 5 or 7))
        {
            throw new InvalidDataException($"The device returned an unknown LED status value: {led}.");
        }

        return new(frequency, minimum, maximum, attenuation, inputMode, triggerMode,
            internalTrigger == "E", standby == "E", modulation, gain,
            led is 5 or 7, led is 3 or 7, true);
    }

    public async Task<int> ApplyChangesAsync(
        G6DeviceState original,
        G6DeviceState updated,
        CancellationToken cancellationToken = default)
    {
        var applied = 0;
        if (original.FrequencyMhz != updated.FrequencyMhz)
        {
            await SetAsync($"FR {updated.FrequencyMhz.ToString("0.000", CultureInfo.InvariantCulture)}", cancellationToken);
            applied++;
        }
        if (original.Attenuation != updated.Attenuation)
        {
            await SetAsync($"AT {updated.Attenuation}", cancellationToken);
            applied++;
        }
        var modulationSourceApplied = false;
        if (original.ModulationSource == "E" && updated.ModulationSource != "E" &&
            original.InputMode != updated.InputMode)
        {
            await SetAsync($"MS {updated.ModulationSource}", cancellationToken);
            applied++;
            modulationSourceApplied = true;
        }
        if (original.InputMode != updated.InputMode)
        {
            await SetAsync($"IM {updated.InputMode}", cancellationToken);
            applied++;
        }
        if (original.TriggerMode != updated.TriggerMode)
        {
            await SetAsync($"TM {updated.TriggerMode}", cancellationToken);
            applied++;
        }
        if (original.InternalTriggerEnabled != updated.InternalTriggerEnabled && updated.InputMode == "M")
        {
            await SetAsync($"IT {(updated.InternalTriggerEnabled ? "E" : "D")}", cancellationToken);
            applied++;
        }
        if (original.RfStandbyEnabled != updated.RfStandbyEnabled)
        {
            await SetAsync($"RS {(updated.RfStandbyEnabled ? "E" : "D")}", cancellationToken);
            applied++;
        }
        if (!modulationSourceApplied && original.ModulationSource != updated.ModulationSource)
        {
            await SetAsync($"MS {updated.ModulationSource}", cancellationToken);
            applied++;
        }
        if (original.ModulationGain != updated.ModulationGain)
        {
            await SetAsync($"MG {updated.ModulationGain}", cancellationToken);
            applied++;
        }

        return applied;
    }

    public Task SetFrequencyAsync(decimal frequencyMhz, CancellationToken cancellationToken = default) =>
        SetAsync($"FR {frequencyMhz.ToString("0.000", CultureInfo.InvariantCulture)}", cancellationToken);

    public Task LoadAsync(CancellationToken cancellationToken = default) => SetAsync("LD", cancellationToken);
    public Task StoreAsync(CancellationToken cancellationToken = default) => SetAsync("ST", cancellationToken);

    private async Task<string> QueryAsync(string command, CancellationToken cancellationToken)
    {
        var result = await serial.ExecuteCommandAsync(command, cancellationToken);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value))
        {
            throw new InvalidDataException(result.Message);
        }
        return result.Value.ToUpperInvariant();
    }

    private async Task<decimal> QueryDecimalAsync(string command, CancellationToken cancellationToken)
    {
        var value = await QueryAsync(command, cancellationToken);
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidDataException($"{command} returned an invalid numeric value: {value}.");
        }
        return parsed;
    }

    private async Task<int> QueryIntAsync(string command, CancellationToken cancellationToken) =>
        decimal.ToInt32(await QueryDecimalAsync(command, cancellationToken));

    private async Task SetAsync(string command, CancellationToken cancellationToken)
    {
        var result = await serial.ExecuteCommandAsync(command, cancellationToken);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Message);
        }
    }
}
