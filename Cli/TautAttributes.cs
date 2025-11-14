using Lg2.Sharpy;
using Microsoft.Extensions.Logging;
using ZLogger;
using static Git.Taut.GitAttrConstants;

namespace Git.Taut;

class TautAttributes(ILogger<TautAttributes> logger)
{
    internal int GetDeltaEncodingEnablingSize(
        string pathName,
        Lg2Repository repo,
        Lg2AttrOptions hostAttrOpts
    )
    {
        var attrVal = repo.GetDeltaEncodingEnablingSizeAttrValue(pathName, hostAttrOpts);

        if (attrVal.IsUnset)
        {
            return DELTA_ENCODING_ENABLING_SIZE_DISABLED_VALUE;
        }

        if (attrVal.IsSpecified)
        {
            var strVal = attrVal.GetString();

            if (int.TryParse(strVal, out var intVal) == false)
            {
                logger.ZLogWarning(
                    $"{GitAttrHelpers.DeltaEncodingEnablingSizeAttrName} is specified with an invalid value '{strVal}' for '{pathName}', switch to using default value"
                );

                return DELTA_ENCODING_ENABLING_SIZE_DEFAULT_VALUE;
            }

            if (intVal < DELTA_ENCODING_ENABLING_SIZE_LOWER_BOUND)
            {
                logger.ZLogWarning(
                    $"{GitAttrHelpers.DeltaEncodingEnablingSizeAttrName} is specified but less than lower bound {DELTA_ENCODING_ENABLING_SIZE_LOWER_BOUND} for '{pathName}', switch to using default value"
                );

                return DELTA_ENCODING_ENABLING_SIZE_DEFAULT_VALUE;
            }

            return intVal;
        }

        if (attrVal.IsSet)
        {
            logger.ZLogWarning(
                $"{GitAttrHelpers.DeltaEncodingEnablingSizeAttrName} is set but not specified for '{pathName}', switch to using default value"
            );

            return DELTA_ENCODING_ENABLING_SIZE_DEFAULT_VALUE;
        }

        return DELTA_ENCODING_ENABLING_SIZE_DEFAULT_VALUE;
    }

    internal double GetDeltaEncodingTargetRatio(
        string pathName,
        Lg2Repository repo,
        Lg2AttrOptions hostAttrOpts
    )
    {
        var attrVal = repo.GetDeltaEncodingTargetRatioAttrValue(pathName, hostAttrOpts);

        if (attrVal.IsUnset)
        {
            return DELTA_ENCODING_TARGET_RATIO_DISABLED_VALUE;
        }

        if (attrVal.IsSpecified)
        {
            var strVal = attrVal.GetString();

            if (int.TryParse(strVal, out var intVal) == false)
            {
                logger.ZLogWarning(
                    $"{GitAttrHelpers.DeltaEncodingTargetRatioAttrName} is specified with an invalid value '{strVal}' for '{pathName}', switch to using default value"
                );

                return DELTA_ENCODING_TARGET_RATIO_DEFAULT_VALUE;
            }

            const int min = (int)(DELTA_ENCODING_TARGET_RATIO_LOWER_BOUND * 100);
            const int max = (int)(DELTA_ENCODING_TARGET_RATIO_UPPER_BOUND * 100);

            if (intVal < min || intVal > max)
            {
                logger.ZLogWarning(
                    $"{GitAttrHelpers.DeltaEncodingTargetRatioAttrName} is specified but not within the range [{min}, {max}] for '{pathName}', switch to using default value"
                );

                return DELTA_ENCODING_TARGET_RATIO_DEFAULT_VALUE;
            }

            var ratio = (double)intVal / 100;

            return ratio;
        }

        if (attrVal.IsSet)
        {
            logger.ZLogWarning(
                $"{GitAttrHelpers.DeltaEncodingTargetRatioAttrName} is set but not specified for '{pathName}', switch to using default value"
            );

            return DELTA_ENCODING_TARGET_RATIO_DEFAULT_VALUE;
        }

        return DELTA_ENCODING_TARGET_RATIO_DEFAULT_VALUE;
    }

    internal double GetCompressionTargetRatio(
        string pathName,
        Lg2Repository repo,
        Lg2AttrOptions hostAttrOpts
    )
    {
        var attrVal = repo.GetTargetCompressionRatioAttrValue(pathName, hostAttrOpts);

        if (attrVal.IsUnset)
        {
            return COMPRESSION_TARGET_RATIO_DISABLED_VALUE;
        }

        if (attrVal.IsSpecified)
        {
            var strVal = attrVal.GetString();

            if (int.TryParse(strVal, out var intVal) == false)
            {
                logger.ZLogWarning(
                    $"{GitAttrHelpers.CompressionTargetRatioAttrName} is specified with an invalid value '{strVal}' for '{pathName}', switch to using default value"
                );

                return COMPRESSION_TARGET_RATIO_DEFAULT_VALUE;
            }

            const int min = (int)(COMPRESSION_TARGET_RATIO_LOWER_BOUND * 100);
            const int max = (int)(COMPRESSION_TARGET_RATIO_UPPER_BOUND * 100);

            if (intVal < min || intVal > max)
            {
                logger.ZLogWarning(
                    $"{GitAttrHelpers.CompressionTargetRatioAttrName} is specified but not within the range [{min}, {max}] for '{pathName}', switch to using default value"
                );

                return COMPRESSION_TARGET_RATIO_DEFAULT_VALUE;
            }

            var ratio = (double)intVal / 100;

            return ratio;
        }

        if (attrVal.IsSet)
        {
            logger.ZLogWarning(
                $"{GitAttrHelpers.CompressionTargetRatioAttrName} is set but not specified for '{pathName}', switch to using default value"
            );

            return COMPRESSION_TARGET_RATIO_DEFAULT_VALUE;
        }

        return COMPRESSION_TARGET_RATIO_DEFAULT_VALUE;
    }
}
