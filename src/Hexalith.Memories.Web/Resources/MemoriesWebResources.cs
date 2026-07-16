// <copyright file="MemoriesWebResources.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Resources;

/// <summary>
/// Marker type for the Memories web UI localized strings (Story 17.2).
/// </summary>
/// <remarks>
/// Consumed by <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/> to resolve the
/// embedded <c>MemoriesWebResources.resx</c> (EN default) and <c>MemoriesWebResources.fr.resx</c>
/// (French) satellites. The ASP.NET Core resource convention uses <c>typeof(T).FullName</c> as the resx
/// base name, so this class lives in <c>Hexalith.Memories.Web.Resources</c> to match the embedded files
/// under <c>Resources/MemoriesWebResources.resx</c>.
/// </remarks>
public sealed class MemoriesWebResources
{
}
