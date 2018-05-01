﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

        private static readonly Dictionary<AlgorithmKindVersionOne, string> s_AlgorithmKindNameMap = new Dictionary<AlgorithmKindVersionOne, string>
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
            SarifLog = new SarifLog(SarifVersion.TwoZeroZero.ConvertToSchemaUri(),
                                    SarifVersion.TwoZeroZero,
                                    new List<Run>());

            foreach (RunVersionOne run in node.Runs)
            {
                VisitRunVersionOne(run);
            }

            return null;
        }

        public static FileData CreateFileData(FileDataVersionOne node)
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
                        fileData.Hashes.Add(CreateHash(hash));
                    }
                }

                if (node.Tags.Count > 0)
                {
                    fileData.Tags.UnionWith(node.Tags);
                }
            }

            return fileData;
        }

        public static LogicalLocation CreateLogicalLocation(LogicalLocationVersionOne node)
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

        public static Hash CreateHash(HashVersionOne node)
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

        public static Rule CreateRule(RuleVersionOne node)
        {
            Rule rule = null;

            if (node != null)
            {
                rule = new Rule
                {
                    Id = node.Id,
                    MessageStrings = node.MessageFormats,
                    Properties = node.Properties
                };

                if (node.Configuration != RuleConfigurationVersionOne.Unknown &&
                    node.DefaultLevel != ResultLevelVersionOne.Default)
                {
                    rule.Configuration = new RuleConfiguration
                    {
                        Enabled = node.Configuration == RuleConfigurationVersionOne.Enabled
                    };

                    rule.Configuration.DefaultLevel = CreateRuleConfigurationDefaultLevel(node.DefaultLevel);
                }

                if (!string.IsNullOrWhiteSpace(node.Name))
                {
                    rule.Name = new Message
                    {
                        Text = node.Name
                    };
                }

                if (!string.IsNullOrWhiteSpace(node.FullDescription))
                {
                    rule.FullDescription = new Message
                    {
                        Text = node.FullDescription
                    };
                }

                if (!string.IsNullOrWhiteSpace(node.ShortDescription))
                {
                    rule.ShortDescription = new Message
                    {
                        Text = node.ShortDescription
                    };
                }

                if (node.HelpUri != null)
                {
                    rule.HelpLocation = new FileLocation
                    {
                        Uri = node.HelpUri
                    };
                }

                if (node.Tags.Count > 0)
                {
                    rule.Tags.UnionWith(node.Tags);
                }
            }

            return rule;
        }

        private static IList<FileLocation> CreateResponseFilesList(IDictionary<string, string> responseFileToContentsDictionary, Run run)
        {
            List<FileLocation> fileLocations = null;

            if (responseFileToContentsDictionary != null)
            {
                fileLocations = new List<FileLocation>();

                foreach (string key in responseFileToContentsDictionary.Keys)
                {
                    var fileLocation = new FileLocation
                    {
                        Uri = new Uri(key, UriKind.RelativeOrAbsolute)
                    };
                    fileLocations.Add(fileLocation);

                    if (run != null && !string.IsNullOrWhiteSpace(responseFileToContentsDictionary[key]))
                    {
                        // We have contents, so mention this file in run.files
                        if (run.Files == null)
                        {
                            run.Files = new Dictionary<string, FileData>();
                        }

                        if (!run.Files.ContainsKey(key))
                        {
                            run.Files.Add(key, new FileData());
                        }

                        FileData responseFile = run.Files[key];

                        responseFile.Contents = new FileContent
                        {
                            Text = responseFileToContentsDictionary[key]
                        };
                        responseFile.FileLocation = fileLocation;
                    }
                }
            }

            return fileLocations;
        }

        public static Invocation CreateInvocation(InvocationVersionOne v1Invocation,
                                           IEnumerable<NotificationVersionOne> v1ToolNotifications,
                                           IEnumerable<NotificationVersionOne> v1ConfigurationNotifications,
                                           Run run)
        {
            Invocation invocation = CreateInvocation(v1Invocation, run);
            IList<Notification> toolNotifications = CreateNotificationsList(v1ToolNotifications);
            IList<Notification> configurationNotifications = CreateNotificationsList(v1ConfigurationNotifications);

            if (toolNotifications?.Count > 0 || configurationNotifications?.Count > 0)
            {
                if (invocation == null)
                {
                    invocation = new Invocation();
                }

                invocation.ToolNotifications = toolNotifications;
                invocation.ConfigurationNotifications = configurationNotifications;
            }

            return invocation;
        }

        public static Invocation CreateInvocation(InvocationVersionOne node, Run run)
        {
            Invocation invocation = null;

            if (node != null)
            {
                invocation = new Invocation
                {
                    Account = node.Account,
                    CommandLine = node.CommandLine,
                    EndTime = node.EndTime,
                    EnvironmentVariables = node.EnvironmentVariables,
                    Machine = node.Machine,
                    ProcessId = node.ProcessId,
                    Properties = node.Properties,
                    ResponseFiles = CreateResponseFilesList(node.ResponseFiles, run),
                    StartTime = node.StartTime,
                    WorkingDirectory = node.WorkingDirectory
                };

                if (!string.IsNullOrWhiteSpace(node.FileName))
                {
                    invocation.ExecutableLocation = new FileLocation
                    {
                        Uri = new Uri(node.FileName, UriKind.RelativeOrAbsolute)
                    };
                }
            }

            return invocation;
        }

        public static IList<Notification> CreateNotificationsList(IEnumerable<NotificationVersionOne> v1Notifications)
        {
            List<Notification> notifications = null;

            if (v1Notifications != null)
            {
                notifications = new List<Notification>();

                foreach (NotificationVersionOne notification in v1Notifications)
                {
                    notifications.Add(CreateNotification(notification));
                }
            }

            return notifications;
        }

        public static Notification CreateNotification(NotificationVersionOne node)
        {
            Notification notification = null;

            if (node != null)
            {
                notification = new Notification
                {
                    Exception = CreateExceptionData(node.Exception),
                    Id = node.Id,
                    Level = CreateNotificationLevel(node.Level),
                    Properties = node.Properties,
                    RuleId = node.RuleId,
                    RuleKey = node.RuleKey,
                    ThreadId = node.ThreadId,
                    Time = node.Time
                };

                if (!string.IsNullOrWhiteSpace(node.Message))
                {
                    notification.Message = new Message
                    {
                        Text = node.Message
                    };
                }
            }

            return notification;
        }

        public static ExceptionData CreateExceptionData(ExceptionDataVersionOne node)
        {
            ExceptionData exceptionData = null;

            if (node != null)
            {
                exceptionData = new ExceptionData
                {
                    Kind = node.Kind,
                    Message = node.Message
                };

                if (node.InnerExceptions != null)
                {
                    exceptionData.InnerExceptions = new List<ExceptionData>();

                    foreach (ExceptionDataVersionOne edvo in node.InnerExceptions)
                    {
                        exceptionData.InnerExceptions.Add(CreateExceptionData(edvo));
                    }
                }
            }

            return exceptionData;
        }

        public override RunVersionOne VisitRunVersionOne(RunVersionOne v1Run)
        {
            if (v1Run != null)
            {
                Run run = new Run()
                {
                    Architecture = v1Run.Architecture,
                    AutomationId = v1Run.AutomationId,
                    BaselineId = v1Run.BaselineId,
                    Id = v1Run.Id,
                    Properties = v1Run.Properties,
                    Results = new List<Result>(),
                    StableId = v1Run.StableId,
                    Tool = CreateTool(v1Run.Tool)
                };

                SarifLog.Runs.Add(run);

                if (v1Run.Files != null)
                {
                    run.Files = new Dictionary<string, FileData>();

                    foreach (var pair in v1Run.Files)
                    {
                        run.Files.Add(pair.Key, CreateFileData(pair.Value));
                    }
                }

                run.Invocations = new List<Invocation>();
                run.Invocations.Add(CreateInvocation(v1Run.Invocation,
                                                     v1Run.ToolNotifications,
                                                     v1Run.ConfigurationNotifications,
                                                     run));

                if (v1Run.LogicalLocations != null)
                {
                    run.LogicalLocations = new Dictionary<string, LogicalLocation>();

                    foreach (var pair in v1Run.LogicalLocations)
                    {
                        run.LogicalLocations.Add(pair.Key, CreateLogicalLocation(pair.Value));
                    }
                }

                if (v1Run.Rules != null)
                {
                    run.Resources = new Resources
                    {
                        Rules = new Dictionary<string, Rule>()
                    };

                    foreach (var pair in v1Run.Rules)
                    {
                        run.Resources.Rules.Add(pair.Key, CreateRule(pair.Value));
                    }
                }

                if (v1Run.Tags.Count > 0)
                {
                    run.Tags.UnionWith(v1Run.Tags);
                }
            }

            return null;
        }

        public static Tool CreateTool(ToolVersionOne node)
        {
            Tool tool = null;

            if (node != null)
            {
                tool = new Tool()
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

                if (node.Tags.Count > 0)
                {
                    tool.Tags.UnionWith(node.Tags);
                }
            }

            return tool;
        }

        public static NotificationLevel CreateNotificationLevel(NotificationLevelVersionOne v1NotificationLevel)
        {
            switch (v1NotificationLevel)
            {
                case NotificationLevelVersionOne.Error:
                    return NotificationLevel.Error;
                case NotificationLevelVersionOne.Note:
                    return NotificationLevel.Note;
                default:
                    return NotificationLevel.Warning;
            }
        }

        public static RuleConfigurationDefaultLevel CreateRuleConfigurationDefaultLevel(ResultLevelVersionOne v1ResultLevel)
        {
            switch (v1ResultLevel)
            {
                case ResultLevelVersionOne.Error:
                    return RuleConfigurationDefaultLevel.Error;
                case ResultLevelVersionOne.Pass:
                    return RuleConfigurationDefaultLevel.Note;
                case ResultLevelVersionOne.Warning:
                    return RuleConfigurationDefaultLevel.Warning;
                default:
                    return RuleConfigurationDefaultLevel.Warning;
            }
        }
    }
}
