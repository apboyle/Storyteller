﻿using System.IO;
using System.Linq;
using FubuCore.Reflection;
using NUnit.Framework;
using Shouldly;
using StoryTeller.Conversion;
using StoryTeller.Model;
using StoryTeller.Results;

namespace StoryTeller.Testing.Model
{
    [TestFixture]
    public class CellTester
    {
        private readonly Fixture fixture = new Fixture();

        [Test]
        public void can_serialize_cell()
        {
            var serializer = new Newtonsoft.Json.JsonSerializer();

            var cell = new Cell(CellHandling.Basic(), "a", typeof(int));

            var writer = new StringWriter();
            serializer.Serialize(writer, cell);

            var json = writer.ToString();
            
            json.ShouldContain("\"key\":\"a\"");
        }

        [Test]
        public void happy_check_for_a_simple_equals_match()
        {
            var values = new StepValues("1");

            values.Store("a", 1);

            Cell.For<int>("a").Check(values, 1)
                .ShouldBe(CellResult.Success("a"));


        }

        [Test]
        public void sad_path_check_for_a_simple_equals_match()
        {
            var values = new StepValues("1");
            

            values.Store("a", 1);

            Cell.For<int>("a").Check(values, 2)
                .ShouldBe(CellResult.Failure("a", "2"));

        }

        [Test]
        public void matches_simply()
        {
            var cell = new Cell(CellHandling.Basic(), "a", typeof(int));
            var values1 = new StepValues("foo");
            values1.Store(cell.Key, 5);

            var values2 = new StepValues("foo");
            values2.Store(cell.Key, 5);

            var values3 = new StepValues("foo");
            values3.Store(cell.Key, 6);

            ShouldBeTestExtensions.ShouldBe(cell.Matches(values1, values2), true);
            ShouldBeTestExtensions.ShouldBe(cell.Matches(values1, values3), false);
        }

        [Test]
        public void matches_array_()
        {
            var cell = new Cell(CellHandling.Basic(), "a", typeof(int[]));
            var values1 = new StepValues("foo");
            values1.Store(cell.Key, new[]{1, 2, 3});

            var values2 = new StepValues("foo");
            values2.Store(cell.Key, new[] { 1, 2, 3 });

            var values3 = new StepValues("foo");
            values3.Store(cell.Key, new[] { 1, 2, 4 });

            ShouldBeTestExtensions.ShouldBe(cell.Matches(values1, values2), true);
            ShouldBeTestExtensions.ShouldBe(cell.Matches(values1, values3), false);
        }

        private StepValues convert(Cell cell, string rawValue)
        {
            var step = new Step("foo");
            if (rawValue != null)
            {
                step.With(cell.Key, rawValue);
            }

            var values = new StepValues("1");

            cell.ConvertValues(step, values);

            return values;
        }

        [Test]
        public void create_with_basic_converter_and_convert_happy_path()
        {
            var cell = Cell.For<int>("a");
            var values = convert(cell, "1");

            values.Get("a").ShouldBe(1);
        }

        [Test]
        public void NULL_is_converted_as_wait_for_it_null()
        {
            var cell = Cell.For<string>("foo");
            var values = convert(cell, "NULL");
            values.Get("foo").ShouldBeNull();
        }

        [Test]
        public void create_with_basic_converter_and_convert_with_errors()
        {
            var cell = Cell.For<int>("a");
            var values = convert(cell, "not a number");

            var result = values.Errors.Single().ShouldBeOfType<CellResult>();
            result.cell.ShouldBe("a");
            result.error.ShouldBe("Invalid Format");
        }

        [Test]
        public void create_with_no_converter()
        {
            Conversions.Basic().FindConverter(typeof(NoConverterForMe))
                .ShouldBeNull();

            var cell = Cell.For<NoConverterForMe>("a");

            var values = convert(cell, "anything");

            var result = values.Errors.Single().ShouldBeOfType<CellResult>();
            result.cell.ShouldBe("a");
            result.error.ShouldBe("No converter found for type " + typeof(NoConverterForMe).FullName);
        }

        [Test]
        public void Cell_picks_up_the_header_attributes()
        {
            var cell = Cell.For<CellTarget>(x => x.City);
            cell.header.ShouldBe("The City");
        }

        [Test]
        public void Cell_picks_up_the_default_value_attribute()
        {
            var cell = Cell.For<CellTarget>(x => x.City);
            cell.DefaultValue.ShouldBe("Cedar Park");
        }

        public class NoConverterForMe
        {
            
        }

        [Test]
        public void use_a_boolean_editor_for_boolean_type()
        {
            var cell = Cell.For<CellTarget>(x => x.IsActive);
            cell.editor.ShouldBe("boolean");
        }

        [Test]
        public void use_a_select_editor_for_an_enum()
        {
            var cell = Cell.For<CellTarget>(x => x.Direction);
            cell.editor.ShouldBe("select");
            cell.options.Select(x => x.value)
                .ShouldHaveTheSameElementsAs("North", "South", "East", "West");
        }

        [Test]
        public void picks_up_the_editor_attribute()
        {
            var cell = Cell.For<CellTarget>(x => x.City);
            cell.editor.ShouldBe("bigtext");
        }

        [Test]
        public void cell_picks_up_selection_values()
        {
            var cell = Cell.For<CellTarget>(x => x.Country);
            cell.editor.ShouldBe("select");
            cell.options.Select(x => x.value)
                .ShouldHaveTheSameElementsAs("United States", "Canada", "Mexico");
        }

        [Test]
        public void cell_picks_up_selection_list_name()
        {
            var cell = Cell.For<CellTarget>(x => x.State);
            cell.editor.ShouldBe("select");

            cell.OptionListName.ShouldBe("States");
        }

        [Test]
        public void if_list_is_not_on_fixture_picks_up_from_cellHandling()
        {
            var handling = CellHandling.Basic();
            handling.Lists["States"].AddValues("TX", "MO", "AR");

            var fixture = new Fixture();
            ShouldBeTestExtensions.ShouldBe(fixture.Lists.Has("States"), false);

            var property = ReflectionHelper.GetProperty<CellTarget>(x => x.State);
            var cell = Cell.For(handling, property, fixture);
            cell.options.Select(x => x.value)
                .ShouldHaveTheSameElementsAs("TX", "MO", "AR");
        }

        [Test]
        public void list_on_fixture_has_precedence()
        {
            var handling = CellHandling.Basic();
            handling.Lists["States"].AddValues("TX", "MO", "AR");

            var fixture = new Fixture();
            fixture.Lists["States"].AddValues("NY", "CT");

            var property = ReflectionHelper.GetProperty<CellTarget>(x => x.State);
            var cell = Cell.For(handling, property, fixture);
            cell.options.Select(x => x.value)
                .ShouldHaveTheSameElementsAs("NY", "CT");
        }

        [Test]
        public void cell_logs_an_invalid_status_when_the_conversion_fails_with_format_exception()
        {
            var cell = Cell.For<CellTarget>(x => x.Number);

            var values = new StepValues("foo");
            var step = new Step("key").With("Number", "a");

            cell.ConvertValues(step, values);

            var result = values.Errors.Single();
            result.cell.ShouldBe("Number");
            result.Status.ShouldBe(ResultStatus.invalid);
        }

        [Test]
        public void cell_logs_a_missing_status_when_the_raw_value_is_not_found_and_no_default()
        {
            var cell = Cell.For<CellTarget>(x => x.Number);

            var values = new StepValues("foo");
            var step = new Step("key");
                //.With("Number", "a");

            cell.ConvertValues(step, values);

            var result = values.Errors.Single();
            result.cell.ShouldBe("Number");
            result.Status.ShouldBe(ResultStatus.missing);
        }

        [Test]
        public void cell_uses_the_default_for_conversion_when_no_value_exists()
        {
            var cell = Cell.For<CellTarget>(x => x.Number);
            cell.DefaultValue = "111";

            var values = new StepValues("foo");
            var step = new Step("key");
            //.With("Number", "a");

            cell.ConvertValues(step, values);

            ShouldBeTestExtensions.ShouldBe(values.Errors.Any(), false);
            values.Get("Number").ShouldBe(111);
        }
    }

    public enum Directions
    {
        North,
        South,
        East,
        West
    }

    public class CellTarget
    {
        [Header("The City")][Default("Cedar Park")][StoryTeller.Editor("bigtext")]
        public string City { get; set; }

        public bool IsActive { get; set; }

        public Directions Direction { get; set; }

        [SelectionList("States")]
        public string State { get; set; }

        [SelectionValues("United States", "Canada", "Mexico")]
        public string Country { get; set; }

        public int Number { get; set; }
    }
}