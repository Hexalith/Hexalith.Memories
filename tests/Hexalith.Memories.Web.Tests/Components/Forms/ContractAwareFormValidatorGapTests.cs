// <copyright file="ContractAwareFormValidatorGapTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Forms;

using Hexalith.Memories.Web.Components.Forms;

using Shouldly;

/// <summary>
/// QA gap coverage for <see cref="ContractAwareFormValidator"/>: required case/text/enum/range field paths,
/// optional-text tolerance, and unbounded numeric ranges that the existing suite does not exercise.
/// </summary>
public sealed class ContractAwareFormValidatorGapTests
{
    [Fact]
    public void Validate_MissingRequiredCase_BlocksDispatch()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(fields: [FormFixtures.Tenant(), FormFixtures.Case(value: " ")]));

        result.HasErrors.ShouldBeTrue();
        result.CanDispatch.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == FormValidationCode.CaseRequired && m.FieldKey == "case");
    }

    [Fact]
    public void Validate_BlankRequiredText_IsFieldRequired()
    {
        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(fields: [FormFixtures.Tenant(), FormFixtures.Case(), FormFixtures.RequiredText(value: " ")]));

        result.CanDispatch.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == FormValidationCode.FieldRequired && m.FieldKey == "query");
    }

    [Fact]
    public void Validate_BlankRequiredEnum_IsFieldRequired()
    {
        MemoriesFormField requiredEnum = new(
            "axis",
            "Form_Tenant_Label",
            MemoriesFormFieldKind.ContractEnum,
            Value: null,
            Required: true,
            AllowedTokens: ["semantic", "syntactic"]);

        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(fields: [FormFixtures.Tenant(), FormFixtures.Case(), requiredEnum]));

        result.CanDispatch.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == FormValidationCode.FieldRequired && m.FieldKey == "axis");
    }

    [Fact]
    public void Validate_BlankRequiredRange_IsFieldRequired()
    {
        MemoriesFormField requiredRange = new(
            "depth",
            "Form_Tenant_Label",
            MemoriesFormFieldKind.NumericRange,
            Value: null,
            Required: true,
            Minimum: 0d,
            Maximum: 10d);

        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(fields: [FormFixtures.Tenant(), FormFixtures.Case(), requiredRange]));

        result.CanDispatch.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == FormValidationCode.FieldRequired && m.FieldKey == "depth");
    }

    [Fact]
    public void Validate_BlankOptionalText_NeverBlocks()
    {
        MemoriesFormField optional = new("note", "Form_Tenant_Label", MemoriesFormFieldKind.OptionalText, Value: null);

        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(fields: [FormFixtures.Tenant(), FormFixtures.Case(), optional]));

        result.HasErrors.ShouldBeFalse();
        result.CanDispatch.ShouldBeTrue();
        result.Messages.ShouldNotContain(m => m.FieldKey == "note");
    }

    [Fact]
    public void Validate_UnboundedNumericRange_AcceptsFiniteValue()
    {
        MemoriesFormField unbounded = new(
            "depth",
            "Form_Tenant_Label",
            MemoriesFormFieldKind.NumericRange,
            Value: "42");

        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(fields: [FormFixtures.Tenant(), FormFixtures.Case(), unbounded]));

        result.Messages.ShouldNotContain(m => m.Code == FormValidationCode.ValueOutOfRange);
        result.CanDispatch.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    public void Validate_NonFiniteNumericValue_BlocksDispatch(string value)
    {
        MemoriesFormField range = new(
            "depth",
            "Form_Tenant_Label",
            MemoriesFormFieldKind.NumericRange,
            Value: value,
            Minimum: 0d,
            Maximum: 10d);

        FormValidationResult result = ContractAwareFormValidator.Validate(
            FormFixtures.Request(fields: [FormFixtures.Tenant(), FormFixtures.Case(), range]));

        result.Messages.ShouldContain(m => m.Code == FormValidationCode.ValueOutOfRange && m.FieldKey == "depth");
        result.CanDispatch.ShouldBeFalse();
    }
}
