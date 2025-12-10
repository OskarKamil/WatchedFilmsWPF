### Java - JavaFX

#### 0.1 
- First scene in JavaFX.
- Titled window.
- Displays the most recent watched film as a Label.

#### 0.2 
- Shows a scrollable TableView with a complete list of films from the file.
- Icon in the titled window.

#### 0.3 
- Quick actions pane.
- Statistics pane.
- Separate pane for TableView.
- About modal window with a link to the GitHub page and program information.

#### 0.4 
- New quick actions buttons with icons.
- Editable cells in the TableView.

#### 0.5 
- IDs for each record.
- TableView columns' width adjusted to the window size.
- All panes adjusted to the window size.
- New pane for settings with a checkbox (not functioning yet).
- More statistics in the statistics pane.

#### 0.6 
- Window is titled with the path to the opened file.
- If changes in the file are not saved, it shows * in front of the name.
- More buttons with icons and separators depending on the category.
- Functioning settings that are loaded and saved when the program starts and ends.
- Program asks if changes should be saved when closing before saving.

#### 0.7 
- Buttons are enabled and disabled depending on the context and state of the program.
- If a cell is selected, it enables the [Delete row] button.
- Changes made in the file enable the [Save] button.
- Fixed paths of reading and saving `config.ini` file for the JAR file.

### C# - .NET WPF

#### 0.8 
- Transition to another programming language.
- New GUI and visuals.
- Downgrade in functionality and compatibility. Some features are not working properly or not working at all.
- Program can be compiled on Windows only.
- Executable file can run without installing any JRE since .NET runtime is installed by default on Windows machines.

#### 0.9 
- The window size and position stay the same between program runs.
- Column order and width fixed.
- Editing cells works now.
- Deleting a single row selects the next row after deletion.
- Closing a new file with unsaved changes will ask if changes should be saved instead of opening the file picker.

#### 0.10 
- First time running the program, the window size and position are more comfortable. An example file should open by default.
- Button to reset columns' width and order to default.
- Opening a new file while the current file has unsaved changes will ask if changes should be saved first.
- Statistics update when adding, removing, or changing film details.
- More statistics details based on which decade the film belongs to.
- Other fixes and minor improvements.

#### 0.11 
- Save icon no longer blurry.
- Table of watched films is now scrollable - works much faster on large lists.
- Settings section moved to the top.
- Table of watched films expands with the window size.
- Fixed statistics update when film details change.
- Fixed columns' width.
- Nicer display of decadal statistics with sortable values.
- Fixed slow updates to decades when removing or adding a record.
- Yearly statistics breakdown added.
- Improved about window.
- Fixed film ID in the table to properly refresh now.
- On a long table with a scrollbar, adding a new record will select and scroll to the new record.
- Improved visuals for the save changes dialog.

#### 0.12 
- When a new version is available, the panel will turn yellow and display information about new version. Hyperlink will open release GitHub page.
- If any button is disabled, the icon of the button will be 50% opaque.
- Check for update button in settings.
- Results of checking the update in settings.
- Checkbox to check update on startup.

#### 0.13
- filesSnapshot folder keeps last 5 saves of a file in case of accidental overwritting or data loss.
- When last opened file is deleted. The title of the window doesn't display old path anymore. It displays new file now.
- New option to automatically scroll to last film record on file open.
- New button that takes you to last film record in a file.
- Statistics works faster on long lists and doesn't block the user interface.
- Slight change in Settings panel.
- New buttons that opens current opened file in file explorer directory.
- New button that opens file explorer with currently opened file.
- New button that opens file chooser of MyData folder.
- New option to store your MyFilms list in MyData folder located in the program directory. This option will ask you if you wanna copy the original file or move it.

#### 0.14
- New search bar to quickly find a film in the list. The list of films will be filtered as you type.
- The program folder contains ReadMe.txt file.
- The example csv file is now named ExampleFile.csv.
- The program doesn't containt unncessary files like .pdb or .config anymore.
- Config file is created with default values if it doesn't exist.
- When updating to the new version, the new version folder doesn't contain config files which would overwrite user preferences.
- New context menu when you right click on a film record.
- New context menu item: it deletes a selected film record.
- New context menu item: it opens a default internet browser with a google result of selected film: English title and release year.
- Now you can select multiple cells in a table. (No use now, but you will be able to delete multiple records at once in the future. WIP)
- The table now highlights the alternating 3 rows with Windows accent colour for better readability.
- The table highlight colour changes when Windows accent colour changes.

#### 0.15
- New tab system. Now more than 1 file can be opened. Each opened file will have its own tab with icon depending on common collection type. The tabs cannot be dragged and reordered at the moment. But common standard actions are supported like closing the tab with X icon or using middle button of a mouse (scroll wheel click) to close a tab.
- New buttons: [Add column], [Remove column], [Rename column], [Save all].
- New behaviour for [New file] button. A context menu will show with common collection templates: Books, Comics, Films, Games, Other, Series.
- New behaviour for [Add record] button. When pressed [Add record] button, the program will start editing first cell.
- New program versatility. The program can read any CSV file. The columns are automatically generated.
- The program can read first line of CSV file as a comment if some metadata are present.
- The program now can add a comment as a first line in CSV file that will incluse metadata about the file like the common collection type or details about which columns are used in statistics panel.
- New Panel on the right showing file information like filepath, delimited, common collection type with an option to change delimited and common collection type.
- New settings option to reopen last opened files on startup.