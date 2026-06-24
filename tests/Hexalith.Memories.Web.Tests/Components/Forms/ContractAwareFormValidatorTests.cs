// <copyright file="ContractAwareFormValidatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Forms;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Forms;

using Shouldly;

public sealed class ContractAwareFormValidatorTests
{
    [Fact]
    public void Validate_NullRequest_Throws()
        => Should.Throw<ArgumentNullException>(() => ContractAwareFormValidator.Validate(null!));

    [Fact]
    public void Validate_ValidRequest_CanDispatchWithNoErrors()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(FormFixtures.Request());

        result.HasErrors.ShouldBeFalse();
        result.RequiresAcknowledgement.ShouldBeFalse();
        result.CanDispatch.ShouldBeTrue();
        result.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_MissingTenant_BlocksDispatch()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(requestedTenant: " ", fields: [FormFixtures.Tenant(" "), FormFixtures.Case()]));

        result.HasErrors.ShouldBeTrue();
        result.CanDispatch.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == FormValidationCode.TenantRequired);
    }

    [Fact]
    public void Validate_UnauthorizedScope_BlocksDispatch()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(isolation: EvidencePacketIsolationStatus.Unauthorized));

        result.Messages.ShouldContain(m =>
            m.Code == FormValidationCode.UnauthorizedScope && m.Classification == FormMessageClassification.Blocking);
        result.CanDispatch.ShouldBeFalse();
    }

    [Fact]
    public void Validate_UnknownIsolationStatus_TreatedAsUnauthorized()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(isolation: EvidencePacketIsolationStatus.Unknown));

        result.Messages.ShouldContain(m => m.Code == FormValidationCode.UnauthorizedScope);
        result.CanDispatch.ShouldBeFalse();
    }

    [Fact]
    public void Validate_UnknownEnumValue_IsContractBoundaryError()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(FormFixtures.Request(fields:
        [
            FormFixtures.Tenant(),
            FormFixtures.Case(),
            FormFixtures.EnumField("axis", "telepathic", "semantic", "syntactic", "graph"),
        ]));

        result.Messages.ShouldContain(m =>
            m.Code == FormValidationCode.UnknownEnumValue && m.FieldKey == "axis");
        result.CanDispatch.ShouldBeFalse();
    }

    [Fact]
    public void Validate_KnownEnumValue_IsAccepted()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(FormFixtures.Request(fields:
        [
            FormFixtures.Tenant(),
            FormFixtures.Case(),
            FormFixtures.EnumField("axis", "Semantic", "semantic", "syntactic", "graph"),
        ]));

        result.Messages.ShouldNotContain(m => m.Code == FormValidationCode.UnknownEnumValue);
    }

    [Theory]
    [InlineData("11")]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    [InlineData("NaN")]
    public void Validate_OutOfRangeOrNonNumeric_BlocksDispatch(string value)
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(FormFixtures.Request(fields:
        [
            FormFixtures.Tenant(),
            FormFixtures.Case(),
            FormFixtures.Range("depth", value, 0d, 10d),
        ]));

        result.Messages.ShouldContain(m => m.Code == FormValidationCode.ValueOutOfRange && m.FieldKey == "depth");
        result.CanDispatch.ShouldBeFalse();
    }

    [Fact]
    public void Validate_InRangeValue_IsAccepted()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(FormFixtures.Request(fields:
        [
            FormFixtures.Tenant(),
            FormFixtures.Case(),
            FormFixtures.Range("depth", "3", 0d, 10d),
        ]));

        result.Messages.ShouldNotContain(m => m.Code == FormValidationCode.ValueOutOfRange);
        result.CanDispatch.ShouldBeTrue();
    }

    [Fact]
    public void Validate_TenantChange_RequiresAcknowledgement()
    {
        FormValidationResult unacknowledged = ContractAwareFormValidator.Validate(
            FormFixtures.Request(requestedTenant: "tenant-b", currentTenant: "tenant-a"));

        unacknowledged.RequiresAcknowledgement.ShouldBeTrue();
        unacknowledged.HasErrors.ShouldBeFalse();
        unacknowledged.CanDispatch.ShouldBeFalse();
        unacknowledged.Messages.ShouldContain(m =>
            m.Code == FormValidationCode.TenantChange && m.Classification == FormMessageClassification.Acknowledgement);

        FormValidationResult acknowledged = ContractAwareFormValidator.Validate(
            FormFixtures.Request(requestedTenant: "tenant-b", currentTenant: "tenant-a", acknowledged: true));

        acknowledged.CanDispatch.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ScopeBroadenedToTenantWide_RequiresAcknowledgement()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(FormFixtures.Request(
            requestedCase: null,
            currentCase: "case-a",
            fields: [FormFixtures.Tenant(), FormFixtures.Case(value: null, required: false)]));

        result.Messages.ShouldContain(m => m.Code == FormValidationCode.ScopeBroadened);
        result.RequiresAcknowledgement.ShouldBeTrue();
        result.CanDispatch.ShouldBeFalse();
    }

    [Fact]
    public void Validate_DangerousToggleOn_RequiresAcknowledgement()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(FormFixtures.Request(
            kind: MemoriesFormKind.Repair,
            fields: [FormFixtures.Tenant(), FormFixtures.Case(), FormFixtures.Toggle("forceRepair", on: true)]));

        result.Messages.ShouldContain(m => m.Code == FormValidationCode.DangerousChange);
        result.RequiresAcknowledgement.ShouldBeTrue();
        result.CanDispatch.ShouldBeFalse();
    }

    [Fact]
    public void Validate_AcknowledgementNeverClearsBlockingError()
    {
        // A blocking error plus a dangerous change: acknowledgement must not unlock dispatch while a hard
        // error remains.
        FormValidationResult result = ContractAwareFormValidator.Validate(FormFixtures.Request(
            requestedTenant: "tenant-b",
            currentTenant: "tenant-a",
            acknowledged: true,
            fields: [FormFixtures.Tenant("tenant-b"), FormFixtures.Case(), FormFixtures.RequiredText(value: " ")]));

        result.HasErrors.ShouldBeTrue();
        result.CanDispatch.ShouldBeFalse();
    }

    [Fact]
    public void Validate_ReordersScopeFieldsFirst()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(FormFixtures.Request(fields:
        [
            FormFixtures.RequiredText(),
            FormFixtures.Case(),
            FormFixtures.Tenant(),
        ]));

        result.OrderedFields[0].Kind.ShouldBe(MemoriesFormFieldKind.TenantScope);
        result.OrderedFields[1].Kind.ShouldBe(MemoriesFormFieldKind.CaseScope);
        result.OrderedFields[2].Kind.ShouldBe(MemoriesFormFieldKind.RequiredText);
    }

    [Fact]
    public void Validate_MessagesCarryOnlyLocalizationKeys_NeverRawValues()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(FormFixtures.Request(fields:
        [
            FormFixtures.Tenant(),
            FormFixtures.Case(),
            FormFixtures.EnumField("axis", "secret-Bearer-token", "semantic"),
        ]));

        foreach (FormValidationMessage message in result.Messages)
        {
            message.MessageKey.ShouldBe(FormResourceKeys.Message(message.Code));
            message.MessageKey.ShouldNotContain("secret");
        }
    }

    [Fact]
    public void Validate_ContractSources_MatchTraceabilityForEveryMessage()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(isolation: EvidencePacketIsolationStatus.Unauthorized));

        result.ContractSources.ShouldNotBeEmpty();
        foreach (FormValidationMessage message in result.Messages)
        {
            foreach (string source in FormValidationTraceability.For(message.Code).ContractSources)
            {
                result.ContractSources.ShouldContain(source);
            }
        }
    }

    [Fact]
    public void Traceability_HasExactlyOneRowPerCodeWithNamedSources()
    {
        FormValidationCode[] codes = Enum.GetValues<FormValidationCode>();

        foreach (FormValidationCode code in codes)
        {
            FormValidationTrace trace = FormValidationTraceability.For(code);
            trace.Code.ShouldBe(code);
            trace.MessageKey.ShouldBe(FormResourceKeys.Message(code));
            trace.ContractSources.ShouldNotBeEmpty();
            trace.ContractSources.ShouldAllBe(static s => !string.IsNullOrWhiteSpace(s));
        }

        FormValidationTraceability.Entries.Count.ShouldBe(codes.Length);
    }
}
