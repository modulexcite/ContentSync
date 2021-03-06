﻿using System;
using System.IO;
using System.Linq;

namespace GuiLabs.FileUtilities
{
    public class Sync
    {
        /// <summary>
        /// Assumes source directory exists. destination may or may not exist.
        /// </summary>
        public static void Directories(string source, string destination, Arguments arguments)
        {
            if (!Directory.Exists(destination))
            {
                FileSystem.CreateDirectory(destination, arguments.WhatIf);
            }

            source = Paths.TrimSeparator(source);
            destination = Paths.TrimSeparator(destination);

            var diff = Folders.DiffFolders(source, destination, arguments.Pattern, compareContents: arguments.UpdateChangedFiles);

            bool changesMade = false;
            int filesFailedToCopy = 0;
            int filesFailedToDelete = 0;
            int foldersFailedToCreate = 0;
            int foldersFailedToDelete = 0;

            if (arguments.CopyLeftOnlyFiles)
            {
                using (Log.MeasureTime("Copying new files"))
                {
                    foreach (var leftOnly in diff.LeftOnlyFiles)
                    {
                        var destinationFilePath = destination + leftOnly;
                        if (!FileSystem.CopyFile(source + leftOnly, destinationFilePath, arguments.WhatIf))
                        {
                            filesFailedToCopy++;
                        }

                        changesMade = true;
                    }
                }
            }

            if (arguments.UpdateChangedFiles)
            {
                using (Log.MeasureTime("Updating changed files"))
                {
                    foreach (var changed in diff.ChangedFiles)
                    {
                        var destinationFilePath = destination + changed;
                        if (!FileSystem.CopyFile(source + changed, destinationFilePath, arguments.WhatIf))
                        {
                            filesFailedToCopy++;
                        }

                        changesMade = true;
                    }
                }
            }
            else if (arguments.DeleteChangedFiles)
            {
                using (Log.MeasureTime("Deleting changed files"))
                {
                    foreach (var changed in diff.ChangedFiles)
                    {
                        var destinationFilePath = destination + changed;
                        if (!FileSystem.DeleteFile(destinationFilePath, arguments.WhatIf))
                        {
                            filesFailedToDelete++;
                        }

                        changesMade = true;
                    }
                }
            }

            if (arguments.DeleteSameFiles)
            {
                using (Log.MeasureTime("Deleting identical files"))
                {
                    foreach (var same in diff.IdenticalFiles)
                    {
                        var destinationFilePath = destination + same;
                        if (!FileSystem.DeleteFile(destinationFilePath, arguments.WhatIf))
                        {
                            filesFailedToDelete++;
                        }

                        changesMade = true;
                    }
                }
            }

            if (arguments.DeleteRightOnlyFiles)
            {
                using (Log.MeasureTime("Deleting extra files"))
                {
                    foreach (var rightOnly in diff.RightOnlyFiles)
                    {
                        var deletedFilePath = destination + rightOnly;
                        if (!FileSystem.DeleteFile(deletedFilePath, arguments.WhatIf))
                        {
                            filesFailedToDelete++;
                        }

                        changesMade = true;
                    }
                }
            }

            int foldersCreated = 0;
            if (arguments.CopyEmptyDirectories)
            {
                using (Log.MeasureTime("Creating folders"))
                {
                    foreach (var leftOnlyFolder in diff.LeftOnlyFolders)
                    {
                        var newFolder = destination + leftOnlyFolder;
                        if (!Directory.Exists(newFolder))
                        {
                            if (!FileSystem.CreateDirectory(newFolder, arguments.WhatIf))
                            {
                                foldersFailedToCreate++;
                            }
                            else
                            {
                                foldersCreated++;
                            }

                            changesMade = true;
                        }
                    }
                }
            }

            int foldersDeleted = 0;
            if (arguments.DeleteRightOnlyDirectories)
            {
                using (Log.MeasureTime("Deleting folders"))
                {
                    foreach (var rightOnlyFolder in diff.RightOnlyFolders)
                    {
                        var deletedFolderPath = destination + rightOnlyFolder;
                        if (Directory.Exists(deletedFolderPath))
                        {
                            if (!FileSystem.DeleteDirectory(deletedFolderPath, arguments.WhatIf))
                            {
                                foldersFailedToDelete++;
                            }
                            else
                            {
                                foldersDeleted++;
                            }

                            changesMade = true;
                        }
                    }
                }
            }

            if (diff.LeftOnlyFiles.Any() && arguments.CopyLeftOnlyFiles)
            {
                if (arguments.WhatIf)
                {
                    Log.WriteLine($"Would have copied {diff.LeftOnlyFiles.Count()} new files", ConsoleColor.Green);
                }
                else
                {
                    Log.WriteLine($"{diff.LeftOnlyFiles.Count()} new files copied", ConsoleColor.Green);
                }
            }

            if (foldersCreated > 0 && arguments.CopyEmptyDirectories)
            {
                if (arguments.WhatIf)
                {
                    Log.WriteLine($"Would have created {foldersCreated} folders", ConsoleColor.Green);
                }
                else
                {
                    Log.WriteLine($"{foldersCreated} folders created", ConsoleColor.Green);
                }
            }

            if (diff.ChangedFiles.Any() && arguments.UpdateChangedFiles)
            {
                if (arguments.WhatIf)
                {
                    Log.WriteLine($"Would have updated {diff.ChangedFiles.Count()} changed files", ConsoleColor.Yellow);
                }
                else
                {
                    Log.WriteLine($"{diff.ChangedFiles.Count()} changed files updated", ConsoleColor.Yellow);
                }
            }

            if (diff.ChangedFiles.Any() && arguments.DeleteChangedFiles)
            {
                if (arguments.WhatIf)
                {
                    Log.WriteLine($"Would have deleted {diff.ChangedFiles.Count()} changed files", ConsoleColor.Yellow);
                }
                else
                {
                    Log.WriteLine($"{diff.ChangedFiles.Count()} changed files deleted", ConsoleColor.Yellow);
                }
            }

            if (diff.RightOnlyFiles.Any() && arguments.DeleteRightOnlyFiles)
            {
                if (arguments.WhatIf)
                {
                    Log.WriteLine($"Would have deleted {diff.RightOnlyFiles.Count()} right-only files", ConsoleColor.Red);
                }
                else
                {
                    Log.WriteLine($"{diff.RightOnlyFiles.Count()} right-only files deleted", ConsoleColor.Red);
                }
            }

            if (foldersDeleted > 0 && arguments.DeleteRightOnlyDirectories)
            {
                if (arguments.WhatIf)
                {
                    Log.WriteLine($"Would have deleted {foldersDeleted} right-only folders", ConsoleColor.Red);
                }
                else
                {
                    Log.WriteLine($"{foldersDeleted} right-only folders deleted", ConsoleColor.Red);
                }
            }

            if (diff.IdenticalFiles.Any())
            {
                if (arguments.DeleteSameFiles)
                {
                    if (arguments.WhatIf)
                    {
                        Log.WriteLine($"Would have deleted {diff.IdenticalFiles.Count()} identical files from destination", ConsoleColor.White);
                    }
                    else
                    {
                        Log.WriteLine($"{diff.IdenticalFiles.Count()} identical files deleted from destination", ConsoleColor.White);
                    }
                }
                else
                {
                    Log.WriteLine($"{diff.IdenticalFiles.Count()} identical files", ConsoleColor.White);
                }
            }

            if (filesFailedToCopy > 0)
            {
                Log.WriteLine($"Failed to copy {filesFailedToCopy} files", ConsoleColor.Red);
            }

            if (filesFailedToDelete > 0)
            {
                Log.WriteLine($"Failed to delete {filesFailedToDelete} files.", ConsoleColor.Red);
            }

            if (foldersFailedToCreate > 0)
            {
                Log.WriteLine($"Failed to create {foldersFailedToCreate} folders.", ConsoleColor.Red);
            }

            if (foldersFailedToDelete > 0)
            {
                Log.WriteLine($"Failed to delete {foldersFailedToDelete} folders.", ConsoleColor.Red);
            }

            if (!changesMade)
            {
                if (arguments.WhatIf)
                {
                    Log.WriteLine("Would have made no changes.", ConsoleColor.White);
                }
                else
                {
                    Log.WriteLine("Made no changes.", ConsoleColor.White);
                }
            }

            // if there were no errors, delete the cache of the folder contents. Otherwise
            // chances are they're going to restart the process, so we might needs the cache
            // next time.
            if (filesFailedToCopy == 0 &&
                filesFailedToDelete == 0 &&
                foldersFailedToCreate == 0 &&
                foldersFailedToDelete == 0)
            {
                DirectoryContentsCache.ClearWrittenFilesFromCache();
            }
        }

        /// <summary>
        /// Assumes source exists, destination may or may not exist.
        /// If it exists and is identical bytes to source, nothing is done.
        /// If it exists and is different, it is overwritten.
        /// If it doesn't exist, source is copied.
        /// </summary>
        public static void Files(string source, string destination, Arguments arguments)
        {
            if (File.Exists(destination) && FileUtilities.Files.AreContentsIdentical(source, destination))
            {
                Log.WriteLine("File contents are identical.", ConsoleColor.White);
                return;
            }

            FileSystem.CopyFile(source, destination, arguments.WhatIf);
        }
    }
}