﻿
namespace FormatValidator
{
    using System.Collections.Generic;
    using FormatValidator.Validators;

    /// <summary>
    /// Converts a <see cref="ValidatorConfiguration"/> in to a <see cref="ConvertedValidators"/>
    /// </summary>
    internal class ConfigurationConvertor
    {
        private ConvertedValidators _converted;
        private ValidatorConfiguration _fromConfig;

        public ConfigurationConvertor(ValidatorConfiguration fromConfig)
        {
            _fromConfig = fromConfig;
            _converted = new ConvertedValidators();
        }

        public ConvertedValidators Convert()
        {
            _converted = new ConvertedValidators();
            
            ConvertProperties();
            ConvertColumns();

            return _converted;
        }

        private void ConvertProperties()
        {
            _converted.RowSeperator = UnescapeString(_fromConfig.RowSeperator);
            _converted.ColumnSeperator = UnescapeString(_fromConfig.ColumnSeperator);
            _converted.Environment = _fromConfig.Environment;
            _converted.HasHeaderRow = _fromConfig.HasHeaderRow;
            _converted.ConnectionStrings = _fromConfig.ConnectionStrings;
            _converted.ChapterId = _fromConfig.ChapterId;
        }

        private void ConvertColumns()
        {
            if (ConfigHasColumns())
            {
                string connectionString = _converted.ConnectionStrings.Dev;

                if (_converted.Environment.Equals("staging"))
                    connectionString = _converted.ConnectionStrings.Staging;
                else if (_converted.Environment.Equals("prod"))
                    connectionString = _converted.ConnectionStrings.Prod;

                foreach (KeyValuePair<int, ColumnValidatorConfiguration> columnConfig in _fromConfig.Columns)
                {
                    List<IValidator> group = new List<IValidator>();

                    if (columnConfig.Value.Unique) group.Add(new UniqueColumnValidator());
                    if (columnConfig.Value.MaxLength > 0) group.Add(new StringLengthValidator(columnConfig.Value.MaxLength));
                    if (!string.IsNullOrWhiteSpace(columnConfig.Value.Pattern))
                    {
                        group.Add(new TextFormatValidator(columnConfig.Value.Code, columnConfig.Value.Pattern));
                    }
                    if (columnConfig.Value.IsNumeric) group.Add(new NumberValidator());
                    if (columnConfig.Value.IsRequired) group.Add(new NotNullableValidator());
                    if (!string.IsNullOrWhiteSpace(columnConfig.Value.Name))
                    {
                        group.Add(new NameValidator(columnConfig.Value.Name));
                    }
                    if (columnConfig.Value.IsUnique)
                    {
                        group.Add(new UniqueValidator(columnConfig.Value.ReferenceCol));
                    }
                    if (columnConfig.Value.IsDate)
                    {
                        group.Add(new DateValidator());
                    }
                    if (columnConfig.Value.IsBoolean)
                    {
                        group.Add(new BooleanValidator());
                    }
                    if (columnConfig.Value.IsEmail)
                    {
                        group.Add(new EmailValidator());
                    }
                    if (columnConfig.Value.IsCurrency)
                    {
                        group.Add(new CurrencyValidator());
                    }
                    if (columnConfig.Value.IsConstituentLookup)
                    {
                        group.Add(new ConstituentValidator(connectionString));
                    }
                    if (columnConfig.Value.IsInterestLookup)
                    {
                        group.Add(new InterestValidator(connectionString, _converted.ChapterId));
                    }

                    _converted.Columns.Add(columnConfig.Key, group);
                }
            }
        }

        private string UnescapeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return System.Text.RegularExpressions.Regex.Unescape(input);
        }

        private bool ConfigHasColumns()
        {
            return _fromConfig.Columns != null && _fromConfig.Columns.Count > 0;
        }
    }
}
