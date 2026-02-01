using Jellyfin.Plugin.Jellydash.Models;

namespace Jellyfin.Plugin.Jellydash.Tests;

/// <summary>
/// Tests for PlaybackEntry business logic methods.
/// </summary>
[Collection("JellydashPluginTests")]
public class PlaybackEntryTests
{
    [Fact]
    public void ShouldTrackInHistory_NullRuntimeTicks_ReturnsTrue()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = null,
            StartPositionTicks = 0,
            EndPositionTicks = 10_000_000L
        };

        var result = entry.ShouldTrackInHistory(20);

        Assert.True(result);
    }

    [Fact]
    public void ShouldTrackInHistory_ZeroRuntimeTicks_ReturnsTrue()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = 0,
            StartPositionTicks = 0,
            EndPositionTicks = 10_000_000L
        };

        var result = entry.ShouldTrackInHistory(20);

        Assert.True(result);
    }

    [Fact]
    public void ShouldTrackInHistory_NullEndPosition_ReturnsFalse()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = 100_000_000L,
            StartPositionTicks = 0,
            EndPositionTicks = null
        };

        var result = entry.ShouldTrackInHistory(20);

        Assert.False(result);
    }

    [Fact]
    public void ShouldTrackInHistory_NullMinResumePct_ReturnsTrue()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = 100_000_000L,
            StartPositionTicks = 0,
            EndPositionTicks = 1_000_000L // Only 1%
        };

        var result = entry.ShouldTrackInHistory(null);

        Assert.True(result);
    }

    [Fact]
    public void ShouldTrackInHistory_ZeroMinResumePct_ReturnsTrue()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = 100_000_000L,
            StartPositionTicks = 0,
            EndPositionTicks = 1_000_000L // Only 1%
        };

        var result = entry.ShouldTrackInHistory(0);

        Assert.True(result);
    }

    [Fact]
    public void ShouldTrackInHistory_BelowMinimumThreshold_ReturnsFalse()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = 100_000_000L,
            StartPositionTicks = 0,
            EndPositionTicks = 10_000_000L // 10%
        };

        var result = entry.ShouldTrackInHistory(20);

        Assert.False(result);
    }

    [Fact]
    public void ShouldTrackInHistory_ExactlyAtMinimumThreshold_ReturnsTrue()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = 100_000_000L,
            StartPositionTicks = 0,
            EndPositionTicks = 20_000_000L // Exactly 20%
        };

        var result = entry.ShouldTrackInHistory(20);

        Assert.True(result);
    }

    [Fact]
    public void ShouldTrackInHistory_AboveMinimumThreshold_ReturnsTrue()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = 100_000_000L,
            StartPositionTicks = 0,
            EndPositionTicks = 50_000_000L // 50%
        };

        var result = entry.ShouldTrackInHistory(20);

        Assert.True(result);
    }

    [Fact]
    public void ShouldTrackInHistory_WithNonZeroStartPosition_CalculatesCorrectly()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = 100_000_000L,
            StartPositionTicks = 10_000_000L, // Started at 10%
            EndPositionTicks = 35_000_000L // Ended at 35%, watched 25%
        };

        var result = entry.ShouldTrackInHistory(20);

        // Watched 25% (from 10% to 35%), which is >= 20%
        Assert.True(result);
    }

    [Fact]
    public void ShouldTrackInHistory_WithNonZeroStartPosition_BelowThreshold_ReturnsFalse()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = 100_000_000L,
            StartPositionTicks = 10_000_000L, // Started at 10%
            EndPositionTicks = 25_000_000L // Ended at 25%, watched only 15%
        };

        var result = entry.ShouldTrackInHistory(20);

        // Watched only 15% (from 10% to 25%), which is < 20%
        Assert.False(result);
    }

    [Fact]
    public void ShouldTrackInHistory_NearBoundary_RoundingDoesNotAffectResult()
    {
        var entry = new PlaybackEntry
        {
            PlaybackId = Guid.NewGuid(),
            RuntimeTicks = 100_000_000L,
            StartPositionTicks = 0,
            EndPositionTicks = 19_999_999L // Just below 20%
        };

        var belowThreshold = entry.ShouldTrackInHistory(20);
        Assert.False(belowThreshold);

        entry.EndPositionTicks = 20_000_000L; // Exactly 20%
        var atThreshold = entry.ShouldTrackInHistory(20);
        Assert.True(atThreshold);

        entry.EndPositionTicks = 20_000_001L; // Just above 20%
        var aboveThreshold = entry.ShouldTrackInHistory(20);
        Assert.True(aboveThreshold);
    }
}
