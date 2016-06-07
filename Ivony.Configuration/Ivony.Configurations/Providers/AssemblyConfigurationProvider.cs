﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.IO;

namespace Ivony.Configurations
{
  public sealed class AssemblyConfigurationProvider : ConfigurationProvider
  {
    public override JObject GetConfigurationData()
    {
      var attributes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany( item => item.GetCustomAttributes( typeof( ConfigurationFileAttribute ), false )
          .Select( attribute => new { Assembly = item, Attribute = (ConfigurationFileAttribute) attribute } )

        );


      var result = new JObject();
      Dictionary<string, Assembly> sections = new Dictionary<string, Assembly>();

      foreach ( var item in attributes.OrderBy( i => i.Attribute.Section.Length ) )
      {
        var sectionString = string.Join( "/", item.Attribute.Section );
        Assembly conflictAssembly;
        if ( sections.TryGetValue( sectionString, out conflictAssembly ) )
          throw new Exception( string.Format( "Configuration section {0} confilict, it's registered by assembly \"{1}\" and \"{2}\"", sectionString, conflictAssembly.FullName, item.Assembly.FullName ) );

        result.Merge( LoadConfigurationData( item.Assembly, item.Attribute.Filename, item.Attribute.Section ) );
      }


      return result;
    }

    private JObject LoadConfigurationData( Assembly assembly, string filename, string[] section )
    {

      var resourceName = assembly.GetManifestResourceNames().Where( item => item.EndsWith( filename, StringComparison.OrdinalIgnoreCase ) ).FirstOrDefault();
      if ( resourceName == null )
        return null;


      var result = new JObject();
      var data = result;

      foreach ( var key in section )
        data.Add( key, data = new JObject() );


      using ( var reader = new StreamReader( assembly.GetManifestResourceStream( resourceName ), Encoding.UTF8 ) )
      {
        data.Merge( JObject.Parse( reader.ReadToEnd() ) );
      }


      return result;
    }
  }
}