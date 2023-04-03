using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TrUtils.DTOs;

[XmlRoot(ElementName = "testcase")]
public class JunitTestcase
{
    [XmlAttribute(AttributeName = "classname")]
	public string Classname { get; set; }

	[XmlAttribute(AttributeName = "name")]
	public string Name { get; set; }

	[XmlAttribute(AttributeName = "time")]
	public double Time { get; set; }

	[XmlElement(ElementName = "failure")]
	public Failure Failure { get; set; }
}

[XmlRoot(ElementName = "failure")]
public class Failure
{
    [XmlAttribute(AttributeName = "type")]
	public string Type { get; set; }

	[XmlAttribute(AttributeName = "message")]
	public string Message { get; set; }
}

[XmlRoot(ElementName = "testsuite")]
public class JunitTestsuite
{
	[XmlElement(ElementName = "testcase")]
	public List<JunitTestcase> Testcases { get; set; }

	[XmlAttribute(AttributeName = "name")]
	public string Name { get; set; }

	[XmlAttribute(AttributeName = "tests")]
	public int Tests { get; set; }

	[XmlAttribute(AttributeName = "skipped")]
	public int Skipped { get; set; }

	[XmlAttribute(AttributeName = "failures")]
	public int Failures { get; set; }

	[XmlAttribute(AttributeName = "errors")]
	public int Errors { get; set; }

	[XmlAttribute(AttributeName = "time")]
	public double Time { get; set; }

	[XmlAttribute(AttributeName = "timestamp")]
	public DateTime Timestamp { get; set; }

	[XmlAttribute(AttributeName = "hostname")]
	public string Hostname { get; set; }

	[XmlAttribute(AttributeName = "id")]
	public int Id { get; set; }

	[XmlAttribute(AttributeName = "package")]
	public string Package { get; set; }

	[XmlText]
	public string Text { get; set; }
}

[XmlRoot(ElementName = "testsuites")]
public class JunitTestResultDto
{
    [XmlElement(ElementName = "testsuite")]
    public List<JunitTestsuite> JunitTestsuites { get; set; }
}