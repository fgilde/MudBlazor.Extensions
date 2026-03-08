class MudExFileDisplayExcelUniver {
    constructor(elementRef, dotNet, containerId) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.containerId = containerId;
    }

    loadWorkbook(fileBytes) {
        var self = this;
        try {
            var blob = new Blob([new Uint8Array(fileBytes)], {
                type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
            });
            var file = new File([blob], 'workbook.xlsx');

            this.disposeUniver();

            var container = document.getElementById(this.containerId);
            if (!container) {
                this.dotnet.invokeMethodAsync('OnError', 'Container element not found');
                return;
            }

            var createUniver = window.UniverPresets.createUniver;
            var LocaleType = window.UniverCore.LocaleType;
            var mergeLocales = window.UniverCore.mergeLocales;
            var UniverSheetsCorePreset = window.UniverPresetSheetsCore.UniverSheetsCorePreset;

            var localeData = window.UniverPresetSheetsCoreEnUS;
            var locales = {};
            if (localeData) {
                locales[LocaleType.EN_US] = mergeLocales(localeData);
            }

            var result = createUniver({
                locale: LocaleType.EN_US,
                locales: locales,
                presets: [
                    UniverSheetsCorePreset({ container: container })
                ]
            });

            var univerAPI = result.univerAPI;
            this.univerAPI = univerAPI;

            if (window.LuckyExcel && window.LuckyExcel.transformExcelToUniver) {
                window.LuckyExcel.transformExcelToUniver(
                    file,
                    function (workbookData) {
                        if (workbookData) {
                            univerAPI.createWorkbook(workbookData);
                        } else {
                            univerAPI.createWorkbook({ name: 'Workbook' });
                        }
                        self.dotnet.invokeMethodAsync('OnWorkbookLoaded');
                    },
                    function (error) {
                        console.error('LuckyExcel error:', error);
                        univerAPI.createWorkbook({ name: 'Workbook' });
                        self.dotnet.invokeMethodAsync('OnError', 'Failed to parse Excel file: ' + (error || 'Unknown error'));
                    }
                );
            } else {
                univerAPI.createWorkbook({ name: 'Workbook' });
                this.dotnet.invokeMethodAsync('OnError', 'LuckyExcel not available. Showing empty workbook.');
            }
        } catch (e) {
            console.error('Failed to load workbook:', e);
            this.dotnet.invokeMethodAsync('OnError', e.message || 'Failed to load workbook');
        }
    }

    disposeUniver() {
        if (this.univerAPI) {
            try {
                this.univerAPI.dispose();
            } catch (e) {
                console.warn('Error disposing Univer:', e);
            }
            this.univerAPI = null;
        }
    }
     
    dispose() {
        this.disposeUniver();
    }
}

window.MudExFileDisplayExcelUniver = MudExFileDisplayExcelUniver;

export function initializeMudExFileDisplayExcelUniver(elementRef, dotnet, containerId) {
    return new MudExFileDisplayExcelUniver(elementRef, dotnet, containerId);
}
