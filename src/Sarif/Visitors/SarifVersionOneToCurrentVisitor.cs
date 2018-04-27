﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Microsoft.CodeAnalysis.Sarif.VersionOne;

namespace Microsoft.CodeAnalysis.Sarif.Visitors
{
    public class SarifVersionOneToCurrentVisitor : SarifRewritingVisitorVersionOne
    {
        #region Text MIME types
        private static HashSet<string> s_TextMimeTypes = new HashSet<string>()
        {
            "application/ecmascript",
            "application/javascript",
            "application/json",
            "application/rss+xml",
            "application/rtf",
            "application/typescript",
            "application/x-csh",
            "application/xhtml+xml",
            "application/xml",
            "application/x-sh",
            "text/css",
            "text/csv",
            "text/ecmascript",
            "text/html",
            "text/javascript",
            "text/plain",
            "text/richtext",
            "text/sgml",
            "text/tab-separated-values",
            "text/tsv",
            "text/uri-list",
            "text/x-asm",
            "text/x-c",
            "text/x-csharp",
            "text/x-h",
            "text/x-java-source",
            "text/x-java-source,java",
            "text/xml",
            "text/x-pascal"
        };
        #endregion

        private static Dictionary<AlgorithmKindVersionOne, string> s_AlgorithmKindNameMap = new Dictionary<AlgorithmKindVersionOne, string>
        {
            { AlgorithmKindVersionOne.Sha1, "sha-1" },
            { AlgorithmKindVersionOne.Sha3, "sha-3" },
            { AlgorithmKindVersionOne.Sha224, "sha-224" },
            { AlgorithmKindVersionOne.Sha256, "sha-256" },
            { AlgorithmKindVersionOne.Sha384, "sha-384" },
            { AlgorithmKindVersionOne.Sha512, "sha-512" }
        };

        public SarifLog SarifLog { get; private set; }

        public override SarifLogVersionOne VisitSarifLogVersionOne(SarifLogVersionOne node)
        {
            SarifLog = new SarifLog();
            SarifLog.Runs = new List<Run>();
            SarifLog.Version = SarifVersion.TwoZeroZero;

            foreach (RunVersionOne run in node.Runs)
            {
                VisitRunVersionOne(run);
            }

            return null;
        }

        public FileData TransformFileDataVersionOne(FileDataVersionOne node)
        {
            FileData fileData = null;

            if (node != null)
            {
                fileData = new FileData
                {
                    Length = node.Length,
                    MimeType = node.MimeType,
                    Offset = node.Offset,
                    ParentKey = node.ParentKey,
                    Properties = node.Properties                    
                };

                if (fileData.Properties == null)
                {
                    fileData.Properties = new Dictionary<string, SerializedPropertyInfo>();
                }

                if (node.Uri != null)
                {
                    fileData.FileLocation = new FileLocation
                    {
                        Uri = node.Uri,
                        UriBaseId = node.UriBaseId
                    };
                }

                fileData.Contents = new FileContent
                {
                    Binary = node.Contents
                };

                if (s_TextMimeTypes.Contains(node.MimeType))
                {
                    fileData.Contents.Text = node.Contents;
                }

                if (node.Hashes != null)
                {
                    fileData.Hashes = new List<Hash>();

                    foreach (HashVersionOne hash in node.Hashes)
                    {
                        fileData.Hashes.Add(TransformHashVersionOne(hash));
                    }
                }

                fileData.Tags.UnionWith(node.Tags);
            }

            return fileData;
        }

        public LogicalLocation TransformLogicalLocationVersionOne(LogicalLocationVersionOne node)
        {
            LogicalLocation logicalLocation = null;

            if (node != null)
            {
                logicalLocation = new LogicalLocation
                {
                    Kind = node.Kind,
                    Name = node.Name,
                    ParentKey = node.ParentKey
                };
            }

            return logicalLocation;
        }

        public Hash TransformHashVersionOne(HashVersionOne node)
        {
            Hash hash = null;

            if (node != null)
            {
                string algorithm;
                if (!s_AlgorithmKindNameMap.TryGetValue(node.Algorithm, out algorithm))
                {
                    algorithm = node.Algorithm.ToString().ToLowerInvariant();
                }

                hash = new Hash
                {
                    Algorithm = algorithm,
                    Value = node.Value
                };
            }

            return hash;
        }

        public override RunVersionOne VisitRunVersionOne(RunVersionOne node)
        {
            if (node != null)
            {
                Run run = new Run()
                {
                    Architecture = node.Architecture,
                    BaselineId = node.BaselineId,
                    AutomationId = node.AutomationId,
                    Id = node.Id,
                    Properties = node.Properties,
                    Results = new List<Result>(),
                    StableId = node.StableId
                };

                if (run.Properties == null)
                {
                    run.Properties = new Dictionary<string, SerializedPropertyInfo>();
                }

                if (node.Files != null)
                {
                    run.Files = new Dictionary<string, FileData>();

                    foreach (var pair in node.Files)
                    {
                        run.Files.Add(pair.Key, TransformFileDataVersionOne(pair.Value));
                    }
                }

                if (node.LogicalLocations != null)
                {
                    run.LogicalLocations = new Dictionary<string, LogicalLocation>();

                    foreach (var pair in node.LogicalLocations)
                    {
                        run.LogicalLocations.Add(pair.Key, TransformLogicalLocationVersionOne(pair.Value));
                    }
                }
                
                SarifLog.Runs.Add(run);

                VisitToolVersionOne(node.Tool);
            }

            return null;
        }

        public override ResultVersionOne VisitResultVersionOne(ResultVersionOne node)
        {
            return base.VisitResultVersionOne(node);
        }

        public override ToolVersionOne VisitToolVersionOne(ToolVersionOne node)
        {
            if (node != null)
            {
                Tool tool = new Tool()
                {
                    FileVersion = node.FileVersion,
                    FullName = node.FullName,
                    Language = node.Language,
                    Name = node.Name,
                    Properties = node.Properties,
                    SarifLoggerVersion = node.SarifLoggerVersion,
                    SemanticVersion = node.SemanticVersion,
                    Version = node.Version
                };

                if (tool.Properties == null)
                {
                    tool.Properties = new Dictionary<string, SerializedPropertyInfo>();
                }

                tool.Tags.UnionWith(node.Tags);

                SarifLog.Runs.Last().Tool = tool;
            }

            return null;
        }
    }
}
