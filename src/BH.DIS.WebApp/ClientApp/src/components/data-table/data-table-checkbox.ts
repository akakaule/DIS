import { Checkbox, Theme } from "@material-ui/core";
import { withStyles } from "@material-ui/core/styles";

const checkBoxStyles = (theme: Theme) => ({
    root: {
        '&$checked': {
            color: 'rgb(238, 114, 26)',
            "&:hover": {
                backgroundColor: 'rgba(238, 114, 26, 0.08)'
            }
        },
        "&:hover": {
            backgroundColor: 'rgba(238, 114, 26, 0.08)'
        }
    },
    checked: {},
})

export const DataTableCheckbox = withStyles(checkBoxStyles)(Checkbox);