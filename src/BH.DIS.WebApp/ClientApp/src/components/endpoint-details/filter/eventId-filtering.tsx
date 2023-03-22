import * as React from "react";
import * as api from "api-client";
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
            paddingLeft: 16,
        },
        
    }),
);

type EventIdFilteringProps = {

}

const EventIdFiltering = (props: EventIdFilteringProps) => {
    const classes = useStyles();

    const ctx = React.useContext(FilterContext);
    const [eventId, setEventId] = React.useState<string>();


    React.useEffect(() => {
        const filters = ctx.filterContext;
        filters.eventId = eventId;
        ctx.setProjectContext(filters);
    }, [eventId]);

    return (
        <div className={classes.root}>
            <TextField
                id="outlined-basic"
                label="EventId"
                variant="outlined"
                size="small"
                margin="none"
                style={{width: "100%"}}
                onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                    setEventId(event.target.value);
                }}
            />
        </div>)
}

export default EventIdFiltering;