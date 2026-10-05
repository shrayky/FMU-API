import { CheckBox, Number, padding } from "../../../utils/ui.js";
import updateInstallSchedule from "./updateInstallSchedule.js";

const SETTINGS_ID = "githubReleaseUpdateSettings";
const SCHEDULER_PREFIX = "githubScheduler";
const SCHEDULER_SETTINGS_ID = `${SCHEDULER_PREFIX}Settings`;

class GitHubReleaseUpdateElement {
    constructor(id) {
        this.id = id;

        this.enabled = false;
        this.checkIntervalMinutes = 120;
        this.installSchedule = [];

        this.LABELS = {
            enabled: "Использовать",
            checkInterval: "Интервал проверки (минут)",
        };
    }

    loadConfig(config) {
        const settings = config?.githubReleaseUpdate;

        if (settings) {
            this.enabled = settings.enabled ?? false;
            this.checkIntervalMinutes = settings.checkIntervalMinutes ?? 120;
            this.installSchedule = settings.installSchedule ?? [];
        }

        return this;
    }

    render() {
        return {
            id: this.id,
            rows: [
                {
                    padding: padding,
                    rows: [
                        CheckBox(this.LABELS.enabled, "githubReleaseUpdate.enabled", {
                            value: this.enabled,
                            on: {
                                onChange: (enabled) => {
                                    const settings = $$(SETTINGS_ID);
                                    const scheduler = $$(SCHEDULER_SETTINGS_ID);

                                    if (enabled) {
                                        settings.enable();
                                        scheduler.enable();
                                    }
                                    else {
                                        settings.disable();
                                        scheduler.disable();
                                    }
                                }
                            }
                        }),

                        {
                            id: SETTINGS_ID,
                            disabled: !this.enabled,
                            rows: [
                                Number(this.LABELS.checkInterval,
                                    "githubReleaseUpdate.checkIntervalMinutes",
                                    this.checkIntervalMinutes),

                                updateInstallSchedule({
                                    prefix: SCHEDULER_PREFIX,
                                    name: "githubReleaseUpdate.installSchedule",
                                    data: this.installSchedule,
                                    disabled: !this.enabled,
                                }),
                            ],
                        }
                    ],
                }
            ],
        };
    }
}

export default function (id, config) {
    return new GitHubReleaseUpdateElement(id)
        .loadConfig(config)
        .render();
}
