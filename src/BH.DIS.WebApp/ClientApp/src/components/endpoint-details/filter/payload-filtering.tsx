import * as React from "react";
import FilterContext from "./filtering-context";
import { useParams } from "react-router-dom";
import { InputLabel, Box, FormControl, MenuItem, Checkbox, OutlinedInput, ListItemText, TextField } from '@material-ui/core';
import Select from '@material-ui/core/Select';
import Autocomplete from '@material-ui/lab/Autocomplete';
import CheckBoxOutlineBlankIcon from '@material-ui/icons/CheckBoxOutlineBlank';
import CheckBoxIcon from '@material-ui/icons/CheckBox';
import { createStyles, makeStyles, Theme } from '@material-ui/core/styles';

const useStyles = makeStyles((theme: Theme) =>
    createStyles({
        root: {
            width: '-webkit-fill-available',
            height: '-webkit-fill-available',
            paddingLeft: 16,
            paddingTop: '0.6rem'
        },
    }),
);

type PayloadFilteringProps = {

}

const PayloadFiltering = (props: PayloadFilteringProps) => {
    const classes = useStyles();

    const ctx = React.useContext(FilterContext);
    const [payload, setPayload] = React.useState<string>();


    React.useEffect(() => {
        const filters = ctx.filterContext;
        filters.payload = payload;
        ctx.setProjectContext(filters);
    }, [payload]);

    return (
        // <div className={classes.root}>
        <TextField
            multiline={true}
            minRows={3}
            id="outlined-basic"
            label="Payload"
            variant="outlined"

            margin="none"
            style={{ paddingLeft: '16', width: "-webkit-fill-available", height: '-webkit-fill-available', }}
            onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                setPayload(event.target.value);
            }}
        />)
    // </div>)
}

export default PayloadFiltering;