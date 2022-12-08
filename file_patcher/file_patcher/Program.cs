using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.RegularExpressions;


namespace accessibility
{
    class Program
	{
		//Bowie
		//public static string path = @"C:\Users\mattw\Desktop\templates\Bowie\navigation-component-template--before.html";

		//NC
		//public static string path = @"C:\sidearm\files\navigation-component-template--before.html";

		//Ragin Cajuns
		//public static string path = @"C:\Users\mattw\Desktop\templates\ragincajuns\navigation-component-template--before.html";

		//GCU Lopes
		//public static string path = @"C:\Users\mattw\Desktop\templates\gcu\navigation-component-template--before.html";

		//McMurray
		//public static string path = @"C:\Users\mattw\Desktop\templates\McMurray\navigation-component-template--before.html";

		//Marywood
		//public static string path = @"C:\Users\mattw\Desktop\templates\Marywood\navigation-component-template--before.html";

		//RegentRoyals
		//public static string path = @"C:\Users\mattw\Desktop\templates\RegentRoyals\navigation-component-template.html";
		
		//Lemoyne
		//public static string path = @"C:\Users\mattw\Desktop\templates\Lemoyne\navigation-component-template.html";

		//Lemoyne
		public static string path = @"C:\Users\mattw\Desktop\templates\Lehigh\navigation-component-template.html";

		static void Main()
		{ 
			parse_html();
		}

        public static void parse_html()
		{
			var file_text = File.ReadAllText(path);
			var doc = new HtmlDocument();
			doc.LoadHtml(file_text);

			var descendants = doc.DocumentNode.Descendants().Where(d => d.Name != "#text" && d.Name != "#comment").ToList();

			//checking if Off Canvas Nav
			if ((file_text.Contains("$component.isMenuOpen()")) || (descendants[0].Name == "button") || (descendants[1].Name == "button"))
			{ 
				updateOffCanvasNav(doc);
			}

			else
			{
			updateNativeOverflow(doc);
			}

            doc.Save(path + "_copy");
		}

		public static void updateNativeOverflow(HtmlDocument doc)
		{
			var descendants = getKoDescendants(doc);

			var desktopDescendants = getNodesBetweenKo(descendants, doc.DocumentNode.FirstChild.InnerHtml, "");

			updateKoBindingsNativeOverflow(doc, desktopDescendants);

			addEscapeFunctionNativeOverflow(desktopDescendants);

			var mobileDescendants = getNodesBetweenKoMobile(descendants);

			addEscapeFunctionNativeOverflowMobile(mobileDescendants);

			updateKoBindingsNativeOverflowMobile(doc, mobileDescendants);
			
			doc.Save(path + "_copy");
		}

		public static void updateOffCanvasNav(HtmlDocument doc)
		{
		    //update main-nav button data-bind
			var buttonNode = doc.DocumentNode.SelectSingleNode("//button");
			var propName = buttonNode.Attributes.First(a => a.Name == "data-bind").Value;
			var buttonClassName = buttonNode.Attributes.First(a => a.Name == "class").Value; 

			propName = propName.Replace("true", "'true'");
			propName = propName.Replace("false", "'false'");

			buttonNode.SetAttributeValue("data-bind", propName + ", event: { keyup: function(data, event) { if (event.key == 'Escape') { $component.closeMenu(); document.querySelector('.c-navigation__toggle').focus();}}}");

	        //update main-nav <li> data-bind
			var liNode = doc.DocumentNode.SelectSingleNode("//div/div/ul/li");
			propName = liNode.Attributes.First(a => a.Name == "data-bind").Value;

			Regex regex = new Regex("attr: {([^}]+)");
            Match match = regex.Match(propName);
	        string attr = match.Groups[1].Value;

			if (attr != "")
			{ 
				propName = propName.Replace(attr, "'aria-expanded': isItemOpen() ? 'true' : 'false'," + attr);
				liNode.SetAttributeValue("data-bind", propName);
			}

			else
			{
				liNode.SetAttributeValue("data-bind", "'aria-expanded': isItemOpen() ? 'true' : 'false'," + propName);
			}

			var descendants = getKoDescendants(doc);
		
			addEscapeFunction(descendants, buttonClassName);

			updateKoBindingsOffCanvas(doc, descendants);
			
			doc.Save(path + "_copy");
		}

		public static List<HtmlNode> getKoDescendants(HtmlDocument doc)
		{
			var descendants = doc.DocumentNode.Descendants().ToList();

	        return descendants;
		}

		public static List<HtmlNode> getNodesBetweenKoMobile(List<HtmlNode> descendants)
		{
			var buttonNode = descendants.FindAll(x => x.Name == "button" && x.Attributes.Contains("data-bind"));
			var startNode = buttonNode.First(n => n.Attributes.First(x => x.Name == "data-bind").Value.Contains("isMenuOpen()"));
			var startNodeIndex = descendants.FindIndex(x => x.InnerHtml == startNode.InnerHtml);

			var endNodeIndex = descendants.LastIndexOf(descendants.Last());
			
			if (startNodeIndex != -1 && endNodeIndex != 1) 
			{
				var betweenNodes = descendants.GetRange(startNodeIndex, endNodeIndex - startNodeIndex - 1);

				return betweenNodes;
			}

			throw new InvalidOperationException("starting node or ending node not present");
		}

		public static List<HtmlNode> getNodesBetweenKo(List<HtmlNode> descendants, String koBegin, String koEnd)
		{
			var startNode = descendants.Find(x => x.InnerHtml == koBegin);
			var startNodeIndex = descendants.FindIndex(x => x.InnerHtml == koBegin);
			var endNodeIndex = 0;

			if (koEnd == "")
			{ 
			var buttonNode = descendants.FindAll(x => x.Name == "button" && x.Attributes.Contains("data-bind"));
			var endNode = buttonNode.First(n => n.Attributes.First(x => x.Name == "data-bind").Value.Contains("isMenuOpen()"));
			endNodeIndex = descendants.FindIndex(x => x.OuterHtml == endNode.OuterHtml && (x.Line > startNode.Line));
			}

			else
			{ 
			endNodeIndex = descendants.FindIndex(x => x.InnerHtml == koEnd && (x.Line > startNode.Line));
			}

			if (startNodeIndex != -1 && endNodeIndex != 1) 
			{
				var betweenNodes = descendants.GetRange(startNodeIndex + 1, endNodeIndex - startNodeIndex - 1);

				return betweenNodes;
			}

			throw new InvalidOperationException("starting node or ending node not present");
		}

		public static void addEscapeFunctionNativeOverflow(List<HtmlNode> descendants)
		{
			foreach (var descendant in descendants)
			{
				if (descendant.Name == "a")
				{
						var propName = descendant.Attributes.First(a => a.Name == "data-bind").Value;

						//add escape function to <a> data-bind event
						Regex regex = new Regex("blur([^}]+)");
						Match match = regex.Match(propName);
						String attr = match.Groups[1].Value;

						if (descendant.Attributes.Contains("class"))
						{ 
							var className = descendant.Attributes.First(a => a.Name == "class").Value;

							if (className.Contains("level-2") && attr != "")
							{
								if (attr.Contains("$parents[3].closeMenu();"))
								{
									propName = propName.Replace(attr, attr + "}, \nkeyup: function(data, event) { if (event.key == 'Escape') { $parents[3].closeMenu(); $parents[2].closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1'); let focusableElement = parentElement.querySelector('.c-navigation__url--level-1'); focusableElement.focus(); } }");
								}
								else if (attr.Contains("$parents[2].closeMenuItem();"))
								{
									propName = propName.Replace(attr, attr + "}, \nkeyup: function(data, event) { if (event.key == 'Escape') { $parents[2].closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1');  let focusableElement = parentElement.querySelector('.c-navigation__url--level-1'); focusableElement.focus(); }");
								}

								else if (attr.Contains("$parents[1].closeMenuItem();"))
								{
									propName = propName.Replace(attr, attr + "}, \nkeyup: function(data, event) { if (event.key == 'Escape') { $parents[1].closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1');  let focusableElement = parentElement.querySelector('.c-navigation__url--level-1'); focusableElement.focus(); }");
								}

								else
								{ 
									propName = propName.Replace(attr, attr + "}, \nkeyup: function(data, event) { if (event.key == 'Escape') { closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1'); let focusableElement = parentElement.querySelector('.c-navigation__url--level-1'); focusableElement.focus(); } }");
								}

							descendant.SetAttributeValue("data-bind", propName);
							}

							else if (className.Contains("level-2") && attr == "")
							{
								descendant.SetAttributeValue("data-bind", propName + ", \nkeyup: function(data, event) { if (event.key == 'Escape') { closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1'); let focusableElement = parentElement.querySelector('.c-navigation__url--level-1'); focusableElement.focus(); } }");
							}
						}
				}
			}
		}

		public static void addEscapeFunctionNativeOverflowMobile(List<HtmlNode> descendants)
		{
			foreach (var descendant in descendants)
			{
				if (descendant.Name == "a")
				{
					if (descendant.Attributes.Contains("data-bind"))
					{ 
						var propName = descendant.Attributes.First(a => a.Name == "data-bind").Value;

						//add escape function to <a> data-bind event
						Regex regex = new Regex("blur([^}]+)");
						Match match = regex.Match(propName);
						String attr = match.Groups[1].Value;

						if (descendant.Attributes.Contains("class"))
						{
							var className = descendant.Attributes.First(a => a.Name == "class").Value.Split(' ');

							if (attr != "")
							{
								if (attr.Contains("$parents[3].closeMenu();"))
								{
									propName = propName.Replace(attr, attr + "}, \nkeyup: function(data, event) { if (event.key == 'Escape') { $parents[3].closeMenu(); $parents[2].closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1'); let focusableElement = parentElement.querySelector('.c-navigation__url--level-1'); focusableElement.focus(); }");
								}

								else if (attr.Contains("$parents[2].closeMenu();"))
								{
									propName = propName.Replace(attr, attr + "}, \nkeyup: function(data, event) { if (event.key == 'Escape') { $parents[2].closeMenu(); $parents[1].closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1'); let focusableElement = parentElement.querySelector('.c-navigation__url--level-1'); focusableElement.focus(); }");
								}
								
								else if (attr.Contains("$parents[1].closeMenu();"))
								{
									propName = propName.Replace(attr, attr + "}, \nkeyup: function(data, event) { if (event.key == 'Escape') { $parents[[1].closeMenu(); $parent.closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1'); let focusableElement = parentElement.querySelector('.c-navigation__url--level-1); focusableElement.focus(); }");
								}

								else
								{
									propName = propName.Replace(attr, attr + "}, \nkeyup: function(data, event) { if (event.key == 'Escape') { $parent.closeMenu(); document.querySelector('.c-navigation__toggle').focus(); }");
								}

								descendant.SetAttributeValue("data-bind", propName);
							}
						}
					}

					else
					{
						descendant.SetAttributeValue("data-bind", "event: { focus: function() { openMenuItem(); }, blur: function() { closeMenuItem(); }, keyup: function(data, event) { if (event.key == 'Escape') { closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1'); let focusableElement = parentElement.querySelector('.c-navigation__url--level-1'); focusableElement.focus(); } }");
					}
				}
			}
		}

		public static void addEscapeFunction(List<HtmlNode> descendants, String buttonClassName)
		{
			foreach (var descendant in descendants)
			{
				if (descendant.Name == "a")
				{
					var propName = descendant.Attributes.First(a => a.Name == "data-bind").Value;
	
					//add escape function to <a> data-bind event
					Regex regex = new Regex("blur([^}]+)");
					Match match = regex.Match(propName);
					String attr = match.Groups[1].Value;

					if (descendant.Attributes.Contains("class"))
					{ 
						var className = descendant.Attributes.First(a => a.Name == "class").Value.Split(' ');

						if (attr != "")
						{					
							if (attr.Contains("$parents[2].closeMenu();"))
							{
								propName = propName.Replace(attr, attr + $", keyup: function(data, event) {{ if (event.key == 'Escape') {{ $parents[2].closeMenu(); $parents[1].closeMenuItem(); let parentElement = event.currentTarget.closest('.{ (className[1].Contains("level-1") ? buttonClassName : ".c-navigation__url--level-1") }'); let focusableElement = parentElement.querySelector('{ (className[1].Contains("level-1") ? buttonClassName : ".c-navigation__url--level-1") }'); focusableElement.focus(); }} }} ");
							}

							else if (attr.Contains("$parents[1].closeMenu();"))
							{
								propName = propName.Replace(attr, attr + $", keyup: function(data, event) {{ if (event.key == 'Escape') {{ $parents[1].closeMenu(); $parent.closeMenuItem(); let parentElement = event.currentTarget.closest('.{ (className[1].Contains("level-1") ? buttonClassName : ".c-navigation__url--level-1") }'); let focusableElement = parentElement.querySelector('.{ (className[1].Contains("level-1") ? buttonClassName : ".c-navigation__url--level-1") }'); focusableElement.focus(); }} }} ");
							}

							else
							{
								propName = propName.Replace(attr, attr + $", keyup: function(data, event) {{ if (event.key == 'Escape') {{ $parent.closeMenu(); let parentElement = event.currentTarget.closest('.{ (className[1].Contains("level-1") ? buttonClassName : ".c-navigation__url--level-1") }'); let focusableElement = parentElement.querySelector('.{ (className[1].Contains("level-1") ? buttonClassName : ".c-navigation__url--level-1") }'); focusableElement.focus(); }} }} ");
							}

							descendant.SetAttributeValue("data-bind", propName);
						}

						else
						{
							descendant.SetAttributeValue("data-bind", propName + $", event: {{ focus: function() {{ $component.openMenu(); }}, blur: function() {{ $component.closeMenu(); }}, keyup: function(data, event) {{ if (event.key == 'Escape') {{ $parent.closeMenu(); let parentElement = event.currentTarget.closest('.{ (className[1].Contains("level-1") ? buttonClassName : ".c-navigation__url--level-1") }'); let focusableElement = parentElement.querySelector('.{ (className[1].Contains("level-1") ? buttonClassName : ".c-navigation__url--level-1") }'); focusableElement.focus(); }} }} ");
						}
					}
				}
			}
		}

		public static void updateDataBindAttr(String attribute, HtmlNode node)
		{
			Regex regex = new Regex("attr: {([^}]+)");
			Match match = regex.Match(attribute);
			string attr = match.Groups[1].Value;

			if (attr.Contains("isItemOpen()") || !attr.Contains("'true'"))
			{
				attribute = attr.Replace("true", "'true'");
				attribute = attr.Replace("false", "'false'");
			}

			else
			{
				attribute = attribute.Replace(attr, "'aria-expanded': isItemOpen() ? 'true' : 'false'," + attr);
			}

			node.SetAttributeValue("data-bind", attribute);
		}

		public static void updateKoBindingsNativeOverflowMobile(HtmlDocument doc, List<HtmlNode> descendants)
		{
			var buttonClassName = "";

			foreach (var node in descendants)
			{ 

				if (node.Name == "button")
				{
				//update mobile button data-bind
				var propName = node.Attributes.First(a => a.Name == "data-bind").Value;
				buttonClassName = node.Attributes.First(a => a.Name == "class").Value;

				if (!propName.Contains("'true'"))
				{ 
					propName = propName.Replace("true", "'true'");
					propName = propName.Replace("false", "'false'");
					node.SetAttributeValue("data-bind", propName);
				}

				node.SetAttributeValue("data-bind", propName + $", \nevent: {{ keyup: function(data, event) {{ if (event.key == 'Escape') {{ $component.closeMenu(); document.querySelector('.{ buttonClassName }').focus(); }} }}");
				}

				if (node.Name == "div")
				{
					if (node.Attributes.Contains("data-bind") && node.Attributes.First(a => a.Name == "data-bind").Value.Contains("css: isMenuOpen()"))
					{ 
					var propName = node.Attributes.First(a => a.Name == "data-bind").Value;

					var className = node.Attributes.First(a => a.Name == "class").Value;

						if (!propName.Contains("onEscape"))
						{ 
						var attribute = doc.CreateAttribute("onEscape");

						attribute.Value = $"function(){{ $component.closeMenu(); document.querySelector('.{buttonClassName}').focus(); }}";
				
						node.SetAttributeValue("data-bind", propName + attribute.Value);
						}

						else
						{
							propName = propName.Replace("closeMenu", $"function(){{ $component.closeMenu(); document.querySelector('.{buttonClassName}').focus(); }}");
						
							node.SetAttributeValue("data-bind", propName);
						}
					}
				}
			}

				if (descendants.Find(x => x.InnerHtml == "<!-- ko if: items.length > 0 -->") != null || descendants.Find(x => x.InnerHtml == "<!-- ko if: columns.length > 0 -->") != null)
				{
					var nodesBetween = getNodesBetweenKo(descendants, descendants.Find(x => x.InnerHtml == "<!-- ko if: items.length > 0 -->") != null ? "<!-- ko if: items.length > 0 -->" : "<!-- ko if: columns.length > 0 -->", "<!-- /ko -->");

					foreach (var node in nodesBetween)
					{
						if (node.Name == "ul")
						{
							//remove role="menu" from <ul>
							node.Attributes.Remove("role");

							doc.Save(path + "_copy");
						}

						if (node.Name == "span")
						{
							//add role="heading" attribute to <span>
							var attribute = doc.CreateAttribute("role", "heading");
							node.Attributes.Add(attribute);

							//add level="3" attribute to <span>
							attribute = doc.CreateAttribute("aria-level", "3");
							node.Attributes.Add(attribute);

							doc.Save(path + "_copy");
						}
					}
				}

				if (doc.Text.Contains(("<!-- ko if: url === \"\" || url === \"#\" -->")) || (doc.Text.Contains("<!-- ko if: url == \"\" || url == \"#\" -->")))
				{
					var nodesBetween = getNodesBetweenKo(descendants, doc.Text.Contains("<!-- ko if: url === \"\" || url === \"#\" -->") ? "<!-- ko if: url === \"\" || url === \"#\" -->" : "<!-- ko if: url == \"\" || url == \"#\" -->", "<!-- /ko -->");

					foreach (var node in nodesBetween)
					{
						if (node.Name == "a")
						{
							var propName = node.Attributes.First(a => a.Name == "data-bind").Value;

							var className = node.Attributes.First(a => a.Name == "class").Value;

							//replace <a> with <button>
							HtmlNode button = doc.CreateElement("button");

							button = node.ParentNode.ReplaceChild(button, node);

							//set class
							button.SetAttributeValue("class", className);

							var attribute = doc.CreateAttribute("event");

							attribute.Value = $", keyup: function(data, event) {{ if (event.key == 'Escape') {{ $parent.closeMenu(); document.querySelector('.{ className }').focus(); }} }}";

							button.SetAttributeValue("data-bind", propName);
						}
					}
				}
			}

		public static void updateKoBindingsNativeOverflow(HtmlDocument doc, List<HtmlNode> descendants)
		{
			if (descendants.Find(x => x.InnerHtml == "<!-- ko if: url === \"\" -->") != null || descendants.Find( x => x.InnerHtml == "<!-- ko if: url === \"\" || url === \"#\" -->") != null || descendants.Find(x => x.InnerHtml == "<!-- ko if: url == \"\" || url == \"#\" -->") != null)
			{
				var nodesBetween = getNodesBetweenKo(descendants, descendants.Find(x => x.InnerHtml == "<!-- ko if: url === \"\" -->") != null ? "<!-- ko if: url === \"\" -->" : descendants.Find(x => x.InnerHtml == "<!-- ko if: url === \"\" || url === \"#\" -->") != null ? "<!-- ko if: url === \"\" || url === \"#\" -->" : "<!--ko if: url == \"\" || url == \"#\" -->", "<!-- /ko -->");

				foreach (var node in nodesBetween)
				{
					if (node.Name == "a")
					{
						var propName = node.Attributes.First(a => a.Name == "data-bind").Value;

						var className = node.Attributes.First(a => a.Name == "class").Value;

						var innerHtml = node.InnerHtml;

						//replace <a> with <button>
						HtmlNode button = doc.CreateElement("button");

						button = node.ParentNode.ReplaceChild(button, node);

						//set class
						button.SetAttributeValue("class", className);

						button.SetAttributeValue("data-bind", propName);

						button.InnerHtml = innerHtml;

						button.Attributes.Append("tabindex", "0");

						doc.Save(path + "_copy");
					}
				}
			}

			if (descendants.Find(x => x.InnerHtml == "<!-- ko if: url != \"\" -->") != null || descendants.Find(x => x.InnerHtml == "<!-- ko if: url !== \"\" && url !== \"#\" -->") != null)
			{
				var nodesBetween = getNodesBetweenKo(descendants, descendants.Find(x => x.InnerHtml == "<!-- ko if: url != \"\" -->") != null ? "<!-- ko if: url != \"\" -->" : "<!-- ko if: url !== \"\" && url !== \"#\" -->", "<!-- /ko -->");

				foreach (var node in nodesBetween)
				{
					if (node.Name == "a")
					{
						//add class="title" to <span>
						var spanNode = node.SelectSingleNode("span");

						if (spanNode != null)
						{
							if (!spanNode.Attributes.Contains("class"))
							{ 
							var attribute = doc.CreateAttribute("class", "title");
							spanNode.Attributes.Prepend(attribute);
							}

							//insert <span> tag
							spanNode = doc.CreateElement("span");
							spanNode.SetAttributeValue("class", "ally-hide");
							spanNode.InnerHtml = "Opens in new window";

							node.AppendChild(spanNode);

							//wrap <span> in new ko: if
							HtmlNode newChild = doc.CreateTextNode("<!-- ko if: open_in_new_window -->");

							node.InsertBefore(newChild, spanNode);

							newChild = doc.CreateTextNode("<!-- /ko -->");

							node.InsertAfter(newChild, spanNode);

						}
						doc.Save(path + "_copy");
					}
				}
			}

			if (descendants.Find(x => x.InnerHtml == "<!-- ko if: items.length > 0 -->") != null || descendants.Find(x => x.InnerHtml == "<!-- ko if: columns.length > 0 -->") != null)
			{
				var nodesBetween = getNodesBetweenKo(descendants, descendants.Find(x => x.InnerHtml == "<!-- ko if: items.length > 0 -->") != null ? "<!-- ko if: items.length > 0 -->" : "<!-- ko if: columns.length > 0 -->", "<!-- /ko -->");

				foreach (var node in nodesBetween)
				{
					if (node.Name == "ul")
					{
						//remove role="menu" from <ul>
						node.Attributes.Remove("role");

						doc.Save(path + "_copy");
					}

					if (node.Name == "span")
					{
						//add role="heading" attribute to <span>
						var attribute = doc.CreateAttribute("role", "heading");
						node.Attributes.Add(attribute);

						//add level="3" attribute to <span>
						attribute = doc.CreateAttribute("aria-level", "3");
						node.Attributes.Add(attribute);

						doc.Save(path + "_copy");
					}
				}
			}

			if (descendants.Find(x => x.InnerHtml == "<!-- ko if: separator -->") != null)
			{
				var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko if: separator -->", "<!-- /ko -->");

				foreach (var node in nodesBetween)
				{ 
					if (node.Name == "span")
					{
						//add role="heading" attribute to <span>
						var attribute = doc.CreateAttribute("role", "heading");
						node.Attributes.Add(attribute);

						//add level="3" attribute to <span>
						attribute = doc.CreateAttribute("aria-level", "3");
						node.Attributes.Add(attribute);

						doc.Save(path + "_copy");
					}
				}
			}

			if (doc.Text.Contains("<!-- ko ifnot: separator -->"))
			{
				var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko ifnot: separator -->", "<!-- /ko -->");

				foreach (var node in nodesBetween)
				{
					if (node.Name == "a")
					{

					}
				}
			}

			if (doc.Text.Contains("<!-- ko if: schedule_roster_news_links -->"))
			{
				var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko if: schedule_roster_news_links -->", "<!-- /ko -->");

				foreach (var node in nodesBetween)
				{
					if (node.Name == "a")
					{
					
					}
				}
			}

			if (doc.Text.Contains("<!-- ko if: stats -->"))
			{
				var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko if: stats -->", "<!-- /ko -->");

				foreach (var node in nodesBetween)
				{
					if (node.Name == "a")
					{
					
					}
				}
			}

			if (doc.Text.Contains("<!-- ko if: social_media_links -->"))
			{
				var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko if: social_media_links -->", "<!-- /ko -->");

				foreach (var node in nodesBetween)
				{
					if (node.Name == "a")
					{

					}
				}
			}

			if (doc.Text.Contains("<!-- ko if: ad -->"))
			{
				var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko if: ad -->", "<!-- /ko -->");

				foreach (var node in nodesBetween)
				{
					if (node.Name == "a")
					{
						var propName = node.Attributes.First(a => a.Name == "data-bind").Value;

						//add escape function to <a> data-bind event
						Regex regex = new Regex("blur([^}]+)");
						Match match = regex.Match(propName);
						String attr = match.Groups[1].Value;

						if (attr != "")	
						{ 
						propName = propName.Replace(attr, attr + "}, \nkeyup: function(data, event) { if (event.key == 'Escape') { $parent.closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1'); let focusableElement = parentElement.querySelector('.c-navigation__url--level-1'); focusableElement.focus(); } }");
						}

						else
						{
						node.SetAttributeValue("data-bind", ", \nkeyup: function(data, event) { if (event.key == 'Escape') { $parent.closeMenuItem(); let parentElement = event.currentTarget.closest('.c-navigation__item--level-1'); let focusableElement = parentElement.querySelector('.c-navigation__url--level-1'); focusableElement.focus(); } }");
						}

						node.SetAttributeValue("data-bind", propName);
					}
				}
			}
		}

		public static void updateKoBindingsOffCanvas(HtmlDocument doc, List<HtmlNode> descendants)
		{
			if (doc.Text.Contains(("<!-- ko if: url === \"\" || url === \"#\" -->")) || (doc.Text.Contains("<!-- ko if: url == \"\" || url == \"#\" -->")))
			{
				var nodesBetween = getNodesBetweenKo(descendants, doc.Text.Contains("<!-- ko if: url === \"\" || url === \"#\" -->") ? "<!-- ko if: url === \"\" || url === \"#\" -->" : "<!-- ko if: url == \"\" || url == \"#\" -->", "<!-- /ko -->");
				
				foreach (var node in nodesBetween)
				{
					if (node.Name == "a")
					{
						var propName = node.Attributes.First(a => a.Name == "data-bind").Value;

	                    var className = node.Attributes.First(a => a.Name == "class").Value;

						//replace <a> with <button>
						HtmlNode button = doc.CreateElement("button");

						button = node.ParentNode.ReplaceChild(button, node);

						//set class
						button.SetAttributeValue("class", className);

						if (node.Attributes.Contains("href"))
						{ 
						button.SetAttributeValue("href", "javascript: void(0)");
						}

						button.SetAttributeValue("data-bind", propName);

						updateDataBindAttr(propName, button);

						//insert <span> tag
						HtmlNode spanNode = doc.CreateElement("span");
						spanNode.SetAttributeValue("class", "title");
						spanNode.SetAttributeValue("data-bind", "html: title");

						button.AppendChild(spanNode);

						doc.Save(path + "_copy");
					}
				}
			}
			
			if (doc.Text.Contains("<!-- ko if: url !== \"\" && url !== \"#\" -->"))
			{
				var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko if: url !== \"\" && url !== \"#\" -->", "<!-- /ko -->");

				foreach (var node in nodesBetween)
				{
					if(node.Name == "a")
					{ 						
						//add class="title" to <span>
						var spanNode = node.SelectSingleNode("span");

						if (spanNode != null)
						{ 
							var attribute = doc.CreateAttribute("class", "title");
							spanNode.Attributes.Prepend(attribute);

							//insert <span> tag
							spanNode = doc.CreateElement("span");
							spanNode.SetAttributeValue("class", "ally-hide");
							spanNode.InnerHtml = "Opens in new window";

							node.AppendChild(spanNode);

							//wrap <span> in new ko: if
							HtmlNode newChild = doc.CreateTextNode("<!-- ko if: open_in_new_window -->");

							node.InsertBefore(newChild, spanNode);

							newChild = doc.CreateTextNode("<!-- /ko -->");

							node.InsertAfter(newChild, spanNode);

						}
						doc.Save(path + "_copy");
					}
				}
			}

				if (doc.Text.Contains("<!-- ko if: items.length > 0 -->") || doc.Text.Contains("<!-- ko if: columns.length > 0 -->"))
				{
					var nodesBetween = getNodesBetweenKo(descendants, doc.Text.Contains("<!-- ko if: items.length > 0 -->") ? "<!-- ko if: items.length > 0 -->" : "<!-- ko if: columns.length > 0 -->", "<!-- /ko -->");
       
                    foreach (var node in nodesBetween)
					{
						if (node.Name == "ul")
						{
							//remove role="menu" from <ul>
							node.Attributes.Remove("role");

							doc.Save(path + "_copy");
						}
	                    
                        if (node.Name == "span")
                        {
							//add role="heading" attribute to <span>
							var attribute = doc.CreateAttribute("role", "heading");
							node.Attributes.Add(attribute);

							//add level="3" attribute to <span>
							attribute = doc.CreateAttribute("aria-level", "3");
							node.Attributes.Add(attribute);

							doc.Save(path + "_copy");
						}
					}
				}
				
					if (doc.Text.Contains("<!-- ko ifnot: separator -->"))
					{ 
						var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko ifnot: separator -->", "<!-- /ko -->");

						foreach (var node in nodesBetween)
						{
							if (node.Name == "a")
							{ 
						
							}
						}
					}
					
					if (doc.Text.Contains("<!-- ko if: schedule_roster_news_links -->"))
					{
						var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko if: schedule_roster_news_links -->", "<!-- /ko -->");

						foreach (var node in nodesBetween)
						{
							if (node.Name == "a")
							{						
								//insert <span> tag
								HtmlNode spanNode = doc.CreateElement("span");
								spanNode.SetAttributeValue("class", "title");
								spanNode.SetAttributeValue("data-bind", "html: title");

								node.AppendChild(spanNode);

								//insert <span> tag
								spanNode = doc.CreateElement("span");
								spanNode.SetAttributeValue("class", "ally-hide");
								spanNode.InnerHtml = "Opens in new window";

								node.AppendChild(spanNode);

								//wrap <span> in new ko: if
								HtmlNode newChild = doc.CreateTextNode("<!-- ko if: open_in_new_window -->");

								node.InsertBefore(newChild, spanNode);

								newChild = doc.CreateTextNode("<!-- /ko -->");

								node.InsertAfter(newChild, spanNode);

								doc.Save(path + "_copy");
							}
						}
					}

					if (doc.Text.Contains("<!-- ko if: stats -->"))
					{
						var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko if: stats -->", "<!-- /ko -->");

							foreach(var node in nodesBetween)
							{ 
								if (node.Name == "a")
								{
									//insert <span> tag
									HtmlNode spanNode = doc.CreateElement("span");
									spanNode.SetAttributeValue("class", "title");
									spanNode.SetAttributeValue("data-bind", "html: title");

									node.AppendChild(spanNode);

									//insert <span> tag
									spanNode = doc.CreateElement("span");
									spanNode.SetAttributeValue("class", "ally-hide");
									spanNode.InnerHtml = "Opens in new window";

									node.AppendChild(spanNode);

									//wrap <span> in new ko: if
									HtmlNode newChild = doc.CreateTextNode("<!-- ko if: open_in_new_window -->");

									node.InsertBefore(newChild, spanNode);

									newChild = doc.CreateTextNode("<!-- /ko -->");

									node.InsertAfter(newChild, spanNode);

									doc.Save(path + "_copy");

								}
							}
					}

					if (doc.Text.Contains("<!-- ko if: social_media_links -->"))
					{
						var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko if: social_media_links -->", "<!-- /ko -->");

							foreach (var node in nodesBetween)
							{
								if (node.Name == "a")
								{
									
								}
							}
					}

					if (doc.Text.Contains("<!-- ko if: ad -->"))
					{
						var nodesBetween = getNodesBetweenKo(descendants, "<!-- ko if: ad -->", "<!-- /ko -->");

						foreach (var node in nodesBetween)
						{
							if (node.Name == "a")
							{
								var propName = node.Attributes.First(a => a.Name == "data-bind").Value;

								//add escape function to <a> data-bind event
								Regex regex = new Regex("event([^}]+)");
								Match match = regex.Match(propName);
								String attr = match.Groups[1].Value;

								propName = propName.Replace(attr, attr + "}, keyup: function(data, event) { if (event.key == 'Escape') { $component.closeMenu(); document.querySelector('.c-navigation__toggle').focus(); } }");
			
								node.SetAttributeValue("data-bind", propName);
							}
						}
					}
		}
	}
}
