// <copyright file="StoredMetadataField.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Serialization;

/// <summary>Durable representation of one metadata field.</summary>
internal sealed record StoredMetadataField(string Value, MetadataOrigin Origin, float Confidence);
